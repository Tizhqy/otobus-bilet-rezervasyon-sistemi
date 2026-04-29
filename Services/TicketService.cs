using System.Data;
using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IDepartureRepository _departureRepository;

        public TicketService(
            ITicketRepository ticketRepository,
            ISeatRepository seatRepository,
            IDepartureRepository departureRepository)
        {
            _ticketRepository = ticketRepository;
            _seatRepository = seatRepository;
            _departureRepository = departureRepository;
        }

        public async Task<TicketResponseDto?> GetTicketByIdAsync(int ticketId)
        {
            var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
            if (ticket == null) return null;

            return MapToTicketResponseDto(ticket);
        }

        public async Task<IEnumerable<TicketResponseDto>> GetUserTicketsAsync(int userId)
        {
            var tickets = await _ticketRepository.GetByUserIdAsync(userId);
            return tickets.Select(MapToTicketResponseDto);
        }

        public async Task<IEnumerable<TicketResponseDto>> GetAllTicketsAsync()
        {
            var tickets = await _ticketRepository.GetAllAsync();
            return tickets.Select(MapToTicketResponseDto);
        }

        public async Task<TicketResponseDto> PurchaseTicketAsync(int userId, CreateTicketDto createTicketDto)
        {
            return await _ticketRepository.ExecuteInTransactionAsync(async () =>
            {
                // Validate departure exists and is active
                var departure = await _departureRepository.GetByIdWithDetailsAsync(createTicketDto.DepartureId);
                if (departure == null || !departure.IsActive)
                {
                    throw new InvalidOperationException("Departure not found or is not active.");
                }

                if (AppConfig.IsTicketSalesClosed(departure.DepartureTime))
                {
                    throw new InvalidOperationException(
                        $"Ticket sales close {AppConfig.TicketSalesCutoffMinutesBeforeDeparture} minutes before departure.");
                }

                // Validate all seats are available
                var seatIds = createTicketDto.Passengers.Select(p => p.SeatId).ToList();
                if (seatIds.Count != seatIds.Distinct().Count())
                {
                    throw new InvalidOperationException("Ayni koltuk birden fazla yolcuya secilemez.");
                }

                if (!await AreSeatAvailableAsync(createTicketDto.DepartureId, seatIds))
                {
                    throw new InvalidOperationException("One or more selected seats are not available.");
                }

                // Calculate total price
                // Calculate base price dynamically based on remaining seats
                var availableSeats = await _seatRepository.GetAvailableCountAsync(createTicketDto.DepartureId);
                var basePrice = availableSeats <= 10 && availableSeats > 0 ? departure.Price * 1.10m : departure.Price;
                var totalPrice = basePrice * createTicketDto.Passengers.Count;

                // Create ticket
                var ticket = new Ticket
                {
                    UserId = userId,
                    DepartureId = createTicketDto.DepartureId,
                    TotalPrice = totalPrice,
                    Status = TicketStatus.Pending
                };

                await _ticketRepository.CreateAsync(ticket);

                // Book seats and create passengers
                foreach (var passengerDto in createTicketDto.Passengers)
                {
                    // Book the seat
                    var booked = await _seatRepository.BookSeatAsync(passengerDto.SeatId, createTicketDto.DepartureId);
                    if (!booked)
                    {
                        throw new InvalidOperationException($"Failed to book seat {passengerDto.SeatId}.");
                    }

                    // Create passenger
                    var passenger = new Passenger
                    {
                        TicketId = ticket.Id,
                        SeatId = passengerDto.SeatId,
                        FirstName = passengerDto.FirstName,
                        LastName = passengerDto.LastName,
                        IdNumber = passengerDto.IdNumber
                    };

                    await _ticketRepository.CreatePassengerAsync(passenger);
                }

                // Create payment
                var payment = new Payment
                {
                    TicketId = ticket.Id,
                    Amount = totalPrice,
                    Method = Enum.Parse<PaymentMethod>(createTicketDto.Payment.Method, true),
                    Status = PaymentStatus.Pending,
                    TransactionId = null,
                    PaidAt = null
                };

                await _ticketRepository.CreatePaymentAsync(payment);

                // Return the created ticket with details
                var createdTicket = await _ticketRepository.GetByIdWithDetailsAsync(ticket.Id);
                return MapToTicketResponseDto(createdTicket!);
            }, IsolationLevel.RepeatableRead);
        }

        public async Task<bool> CancelTicketAsync(int ticketId, int userId)
        {
            return await _ticketRepository.ExecuteInTransactionAsync(async () =>
            {
                var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);

                if (ticket == null)
                {
                    return false;
                }

                // Verify the ticket belongs to the user
                if (ticket.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You can only cancel your own tickets.");
                }

                // Can't cancel already cancelled tickets
                if (ticket.Status == TicketStatus.Cancelled)
                {
                    throw new InvalidOperationException("Ticket is already cancelled.");
                }

                // Can't cancel tickets when departure is too close
                var minutesUntilDeparture = (ticket.Departure.DepartureTime - DateTime.UtcNow).TotalMinutes;
                if (minutesUntilDeparture <= AppConfig.MinCancellationMinutesBeforeDeparture)
                {
                    return false;
                }

                // Release all seats
                foreach (var passenger in ticket.Passengers)
                {
                    await _seatRepository.ReleaseSeatAsync(passenger.SeatId);
                }

                // Update ticket status
                await _ticketRepository.UpdateStatusAsync(ticketId, TicketStatus.Cancelled);

                // Update payment status to refunded
                if (ticket.Payment != null)
                {
                    var paymentStatus = ticket.Payment.Status == PaymentStatus.Completed
                        ? PaymentStatus.Refunded
                        : PaymentStatus.Failed;
                    await _ticketRepository.UpdatePaymentStatusAsync(ticket.Payment.Id, paymentStatus);
                }

                return true;
            });
        }

        public async Task<bool> ConfirmTicketAsync(int ticketId)
        {
            var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
            if (ticket == null) return false;

            if (ticket.Status != TicketStatus.Pending)
                return false;

            await _ticketRepository.UpdateStatusAsync(ticketId, TicketStatus.Confirmed);
            return true;
        }

        public async Task<bool> CompletePaymentAsync(int ticketId, PaymentMethod paymentMethod, string referenceNo)
        {
            if (ticketId <= 0 || string.IsNullOrWhiteSpace(referenceNo))
                return false;

            var normalizedReference = referenceNo.Trim().ToUpperInvariant();

            return await _ticketRepository.ExecuteInTransactionAsync(async () =>
            {
                var paymentState = await _ticketRepository.GetPaymentStateAsync(ticketId);
                if (paymentState == null || paymentState.Value.PaymentStatus == null)
                    return false;

                if (paymentState.Value.TicketStatus == TicketStatus.Confirmed &&
                    paymentState.Value.PaymentStatus == PaymentStatus.Completed)
                {
                    return string.Equals(
                        paymentState.Value.TransactionId,
                        normalizedReference,
                        StringComparison.OrdinalIgnoreCase);
                }

                if (paymentState.Value.TicketStatus != TicketStatus.Pending ||
                    paymentState.Value.PaymentStatus != PaymentStatus.Pending)
                {
                    return false;
                }

                var completed = await _ticketRepository.TryCompletePaymentAndConfirmTicketAsync(
                    ticketId,
                    paymentMethod,
                    normalizedReference,
                    DateTime.UtcNow);

                if (completed)
                    return true;

                var latestState = await _ticketRepository.GetPaymentStateAsync(ticketId);
                return latestState != null
                    && latestState.Value.TicketStatus == TicketStatus.Confirmed
                    && latestState.Value.PaymentStatus == PaymentStatus.Completed
                    && string.Equals(latestState.Value.TransactionId, normalizedReference, StringComparison.OrdinalIgnoreCase);
            });
        }

        public async Task<bool> UpdateTicketAndPaymentPriceAsync(int ticketId, decimal newPrice)
        {
            return await _ticketRepository.ExecuteInTransactionAsync(async () =>
            {
                var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
                if (ticket == null || ticket.Status != TicketStatus.Pending) return false;

                ticket.TotalPrice = newPrice;
                await _ticketRepository.UpdateAsync(ticket);

                if (ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Pending)
                {
                    ticket.Payment.Amount = newPrice;
                    await _ticketRepository.UpdatePaymentAsync(ticket.Payment);
                }

                return true;
            });
        }

        public async Task<bool> IsSeatAvailableAsync(int departureId, int seatId)
        {
            return await _seatRepository.AreSeatsAvailableAsync(departureId, new[] { seatId });
        }

        public async Task<bool> AreSeatAvailableAsync(int departureId, IEnumerable<int> seatIds)
        {
            return await _seatRepository.AreSeatsAvailableAsync(departureId, seatIds);
        }



        private static TicketResponseDto MapToTicketResponseDto(Ticket ticket)
        {
            return new TicketResponseDto
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                Status = ticket.Status.ToString(),
                TotalPrice = ticket.TotalPrice,
                CreatedAt = ticket.CreatedAt,
                Departure = new DepartureInfoDto
                {
                    Id = ticket.Departure.Id,
                    OriginStation = ticket.Departure.Route.OriginStation.Name,
                    OriginCity = ticket.Departure.Route.OriginStation.City,
                    DestinationStation = ticket.Departure.Route.DestinationStation.Name,
                    DestinationCity = ticket.Departure.Route.DestinationStation.City,
                    DepartureTime = ticket.Departure.DepartureTime,
                    ArrivalTime = ticket.Departure.ArrivalTime,
                    BusPlateNumber = ticket.Departure.Bus.PlateNumber,
                    BusType = ticket.Departure.Bus.Type
                },
                Passengers = ticket.Passengers.Select(p => new PassengerInfoDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    SeatNumber = p.Seat.SeatNumber
                }).ToList(),
                Payment = ticket.Payment != null ? new PaymentResponseDto
                {
                    Id = ticket.Payment.Id,
                    Amount = ticket.Payment.Amount,
                    Method = ticket.Payment.Method.ToString(),
                    Status = ticket.Payment.Status.ToString(),
                    TransactionId = ticket.Payment.TransactionId,
                    PaidAt = ticket.Payment.PaidAt
                } : null
            };
        }
    }
}
