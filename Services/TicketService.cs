using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext _context;

        public TicketService(
            ITicketRepository ticketRepository,
            ISeatRepository seatRepository,
            IDepartureRepository departureRepository,
            AppDbContext context)
        {
            _ticketRepository = ticketRepository;
            _seatRepository = seatRepository;
            _departureRepository = departureRepository;
            _context = context;
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
            // Use transaction for atomic purchase
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate departure exists and is active
                var departure = await _departureRepository.GetByIdWithDetailsAsync(createTicketDto.DepartureId);
                if (departure == null || !departure.IsActive)
                {
                    throw new InvalidOperationException("Departure not found or is not active.");
                }

                if (departure.DepartureTime <= DateTime.Now)
                {
                    throw new InvalidOperationException("Cannot book tickets for past departures.");
                }

                // Validate all seats are available
                var seatIds = createTicketDto.Passengers.Select(p => p.SeatId).ToList();
                if (!await AreSeatAvailableAsync(createTicketDto.DepartureId, seatIds))
                {
                    throw new InvalidOperationException("One or more selected seats are not available.");
                }

                // Calculate total price
                var totalPrice = departure.Price * createTicketDto.Passengers.Count;

                // Create ticket
                var ticket = new Ticket
                {
                    UserId = userId,
                    DepartureId = createTicketDto.DepartureId,
                    TotalPrice = totalPrice,
                    Status = TicketStatus.Confirmed
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
                    Amount = createTicketDto.Payment.Amount,
                    Method = Enum.Parse<PaymentMethod>(createTicketDto.Payment.Method, true),
                    Status = PaymentStatus.Completed,
                    TransactionId = createTicketDto.Payment.TransactionId,
                    PaidAt = DateTime.Now
                };

                await _ticketRepository.CreatePaymentAsync(payment);

                await transaction.CommitAsync();

                // Return the created ticket with details
                var createdTicket = await _ticketRepository.GetByIdWithDetailsAsync(ticket.Id);
                return MapToTicketResponseDto(createdTicket!);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelTicketAsync(int ticketId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
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

                // Can't cancel tickets for past departures
                if (ticket.Departure.DepartureTime <= DateTime.Now)
                {
                    throw new InvalidOperationException("Cannot cancel tickets for past departures.");
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
                    await _ticketRepository.UpdatePaymentStatusAsync(ticket.Payment.Id, PaymentStatus.Refunded);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> IsSeatAvailableAsync(int departureId, int seatId)
        {
            var seat = await _seatRepository.GetByIdAsync(seatId);
            return seat != null
                && seat.DepartureId == departureId
                && seat.Status == SeatStatus.Available;
        }

        public async Task<bool> AreSeatAvailableAsync(int departureId, IEnumerable<int> seatIds)
        {
            foreach (var seatId in seatIds)
            {
                if (!await IsSeatAvailableAsync(departureId, seatId))
                {
                    return false;
                }
            }
            return true;
        }

        private static TicketResponseDto MapToTicketResponseDto(Ticket ticket)
        {
            return new TicketResponseDto
            {
                Id = ticket.Id,
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
