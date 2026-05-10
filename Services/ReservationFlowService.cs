using System.Text.Json;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    /// <summary>
    /// Reservation Flow Service Implementation
    /// Temporary Reservation → Payment → Ticket Confirmation akışını yönetir.
    /// MVC & SoC prensipleri ile, Concurrency sorunlarını çözen sistem.
    /// </summary>
    public class ReservationFlowService : IReservationFlowService
    {
        private readonly ITemporaryReservationRepository _reservationRepo;
        private readonly ISeatRepository _seatRepo;
        private readonly IDepartureRepository _departureRepo;
        private readonly ITicketRepository _ticketRepo;
        private readonly ILogService _logService;

        public ReservationFlowService(
            ITemporaryReservationRepository reservationRepo,
            ISeatRepository seatRepo,
            IDepartureRepository departureRepo,
            ITicketRepository ticketRepo,
            ILogService logService)
        {
            _reservationRepo = reservationRepo;
            _seatRepo = seatRepo;
            _departureRepo = departureRepo;
            _ticketRepo = ticketRepo;
            _logService = logService;
        }

        public async Task<(bool Success, string Message, int? ReservationId)> CreateTemporaryReservationAsync(
            int userId,
            int departureId,
            List<int> seatIds,
            List<(string Name, string Surname, string TCNo)> passengerDetails,
            string? couponCode = null)
        {
            try
            {
                if (seatIds.Count < 1 || seatIds.Count > AppConfig.MaxPassengerPerTicket)
                    return (false, $"Invalid seat count. Maximum: {AppConfig.MaxPassengerPerTicket}", null);

                var existingReservation = await _reservationRepo.GetActiveByUserAndDepartureAsync(userId, departureId);
                if (existingReservation != null)
                    return (false, "You already have an active reservation for this departure.", null);

                var departure = await _departureRepo.GetByIdAsync(departureId);
                if (departure == null)
                    return (false, "Departure not found", null);

                var seatsToReserve = new List<Seat>();
                foreach (var seatId in seatIds)
                {
                    var seat = await _seatRepo.GetByIdAsync(seatId);
                    if (seat == null || seat.DepartureId != departureId || seat.Status != SeatStatus.Available)
                        return (false, $"Seat {seatId} is not available", null);
                    seatsToReserve.Add(seat);
                }

                var totalAmount = CalculateTotalAmount(seatsToReserve, departure);
                var idempotencyKey = Guid.NewGuid().ToString();

                var reservation = new TemporaryReservation
                {
                    UserId = userId,
                    DepartureId = departureId,
                    SelectedSeatIds = JsonSerializer.Serialize(seatIds),
                    PassengerDetails = JsonSerializer.Serialize(passengerDetails),
                    IdempotencyKey = idempotencyKey,
                    TotalAmount = totalAmount,
                    CouponCode = couponCode
                };

                var createdReservation = await _reservationRepo.CreateAsync(reservation);

                foreach (var seat in seatsToReserve)
                    await _seatRepo.UpdateStatusAsync(seat.Id, SeatStatus.Reserved);

                await _logService.LogAsync(userId, "TemporaryReservationCreated", 
                    $"Seats: {seatIds.Count}, Amount: {totalAmount}");

                return (true, "Reservation created. Valid for 15 minutes.", createdReservation.Id);
            }
            catch (Exception ex)
            {
                await _logService.LogAsync(null, "TemporaryReservationError", ex.Message);
                return (false, "An error occurred", null);
            }
        }

        public async Task<(bool Success, string Message, int? TicketId)> ConvertReservationToTicketAsync(
            int reservationId,
            string idempotencyKey)
        {
            try
            {
                var reservation = await _reservationRepo.GetByIdAsync(reservationId);
                if (reservation == null || reservation.ExpiresAt <= DateTime.UtcNow)
                    return (false, "Reservation not found or expired", null);

                var existingReservation = await _reservationRepo.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existingReservation?.Status == "Converted")
                    return (true, "Ticket already created", null);

                var seatIds = JsonSerializer.Deserialize<List<int>>(reservation.SelectedSeatIds) ?? new();
                var passengerDetails = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(reservation.PassengerDetails) ?? new();

                var ticket = new Ticket
                {
                    UserId = reservation.UserId,
                    DepartureId = reservation.DepartureId,
                    TotalPrice = reservation.TotalAmount,
                    Status = TicketStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow
                };

                var createdTicket = await _ticketRepo.CreateAsync(ticket);

                int seatIndex = 0;
                foreach (var seatId in seatIds)
                {
                    var seat = await _seatRepo.GetByIdAsync(seatId);
                    if (seat != null)
                        await _seatRepo.UpdateStatusAsync(seat.Id, SeatStatus.Booked);
                    seatIndex++;
                }

                reservation.Status = "Converted";
                await _reservationRepo.UpdateAsync(reservation);

                await _logService.LogTicketPurchaseAsync(reservation.UserId, createdTicket.Id, "System");

                return (true, "Ticket created successfully", createdTicket.Id);
            }
            catch (Exception ex)
            {
                await _logService.LogAsync(null, "ConversionError", ex.Message);
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public async Task<int> CleanupExpiredReservationsAsync()
        {
            try
            {
                var expiredReservations = await _reservationRepo.GetExpiredAsync();
                int cleanedCount = 0;

                foreach (var reservation in expiredReservations)
                {
                    var seatIds = JsonSerializer.Deserialize<List<int>>(reservation.SelectedSeatIds) ?? new();
                    foreach (var seatId in seatIds)
                    {
                        var seat = await _seatRepo.GetByIdAsync(seatId);
                        if (seat?.Status == SeatStatus.Reserved)
                            await _seatRepo.UpdateStatusAsync(seat.Id, SeatStatus.Available);
                    }

                    await _reservationRepo.DeleteAsync(reservation.Id);
                    cleanedCount++;
                    await _logService.LogAsync(null, "ReservationExpired", $"Reservation {reservation.Id} expired");
                }

                return cleanedCount;
            }
            catch (Exception ex)
            {
                await _logService.LogAsync(null, "CleanupError", ex.Message);
                return 0;
            }
        }

        public async Task<bool> CancelReservationAsync(int reservationId)
        {
            try
            {
                var reservation = await _reservationRepo.GetByIdAsync(reservationId);
                if (reservation == null) return false;

                var seatIds = JsonSerializer.Deserialize<List<int>>(reservation.SelectedSeatIds) ?? new();
                foreach (var seatId in seatIds)
                {
                    var seat = await _seatRepo.GetByIdAsync(seatId);
                    if (seat != null)
                        await _seatRepo.UpdateStatusAsync(seat.Id, SeatStatus.Available);
                }

                await _reservationRepo.DeleteAsync(reservationId);
                await _logService.LogAsync(reservation.UserId, "ReservationCancelled", $"Reservation {reservationId} cancelled");

                return true;
            }
            catch { return false; }
        }

        public async Task<TemporaryReservation?> GetReservationAsync(int reservationId)
        {
            return await _reservationRepo.GetByIdAsync(reservationId);
        }

        private decimal CalculateTotalAmount(List<Seat> seats, Departure departure)
        {
            var basePrice = departure.Price;
            var seatCount = seats.Count;
            var surgePrice = seatCount < 10 ? basePrice * 1.1m : basePrice;
            return surgePrice * seatCount;
        }
    }
}
