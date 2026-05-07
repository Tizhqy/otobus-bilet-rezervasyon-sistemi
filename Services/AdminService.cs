using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IDepartureRepository _departureRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly ITicketRepository _ticketRepository;

        public AdminService(
            AppDbContext context,
            IUserRepository userRepository,
            IDepartureRepository departureRepository,
            ISeatRepository seatRepository,
            ITicketRepository ticketRepository)
        {
            _context = context;
            _userRepository = userRepository;
            _departureRepository = departureRepository;
            _seatRepository = seatRepository;
            _ticketRepository = ticketRepository;
        }

        // User Management
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetUsersPageAsync(string? search, int page, int pageSize)
        {
            return await _userRepository.GetPagedAsync(search, page, pageSize);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            return await _userRepository.CreateAsync(user);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            return await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        public async Task<bool> ToggleUserStatusAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        // Bus Management
        public async Task<IEnumerable<Bus>> GetAllBusesAsync()
        {
            return await _departureRepository.GetAllBusesAsync();
        }

        public async Task<Bus?> GetBusByIdAsync(int id)
        {
            return await _departureRepository.GetBusByIdAsync(id);
        }

        public async Task<Bus> CreateBusAsync(Bus bus)
        {
            return await _departureRepository.CreateBusAsync(bus);
        }

        public async Task<Bus> UpdateBusAsync(Bus bus)
        {
            return await _departureRepository.UpdateBusAsync(bus);
        }

        public async Task<bool> DeleteBusAsync(int id)
        {
            return await _departureRepository.DeleteBusAsync(id);
        }

        public async Task<bool> ToggleBusStatusAsync(int id)
        {
            var bus = await _departureRepository.GetBusByIdAsync(id);
            if (bus == null) return false;

            bus.IsActive = !bus.IsActive;
            await _departureRepository.UpdateBusAsync(bus);
            return true;
        }

        // Route Management
        public async Task<IEnumerable<Route>> GetAllRoutesAsync()
        {
            return await _departureRepository.GetAllRoutesAsync();
        }

        public async Task<Route?> GetRouteByIdAsync(int id)
        {
            return await _departureRepository.GetRouteByIdAsync(id);
        }

        public async Task<Route> CreateRouteAsync(Route route)
        {
            return await _departureRepository.CreateRouteAsync(route);
        }

        public async Task<Route> UpdateRouteAsync(Route route)
        {
            return await _departureRepository.UpdateRouteAsync(route);
        }

        public async Task<bool> DeleteRouteAsync(int id)
        {
            return await _departureRepository.DeleteRouteAsync(id);
        }

        public async Task<bool> ToggleRouteStatusAsync(int id)
        {
            var route = await _departureRepository.GetRouteByIdAsync(id);
            if (route == null) return false;

            route.IsActive = !route.IsActive;
            await _departureRepository.UpdateRouteAsync(route);
            return true;
        }

        // Station Management
        public async Task<IEnumerable<Station>> GetAllStationsAsync()
        {
            return await _departureRepository.GetAllStationsAsync();
        }

        public async Task<Station?> GetStationByIdAsync(int id)
        {
            return await _departureRepository.GetStationByIdAsync(id);
        }

        public async Task<Station> CreateStationAsync(Station station)
        {
            return await _departureRepository.CreateStationAsync(station);
        }

        public async Task<Station> UpdateStationAsync(Station station)
        {
            return await _departureRepository.UpdateStationAsync(station);
        }

        public async Task<bool> DeleteStationAsync(int id)
        {
            return await _departureRepository.DeleteStationAsync(id);
        }

        public async Task<bool> ToggleStationStatusAsync(int id)
        {
            var station = await _departureRepository.GetStationByIdAsync(id);
            if (station == null) return false;

            station.IsActive = !station.IsActive;
            await _departureRepository.UpdateStationAsync(station);
            return true;
        }

        // Departure Management
        public async Task<IEnumerable<Departure>> GetAllDeparturesAsync()
        {
            return await _departureRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Departure>> GetUpcomingDeparturesAsync(int count = 100)
        {
            if (count <= 0) count = 100;
            return await _departureRepository.GetUpcomingAsync(count);
        }

        public async Task<(IReadOnlyList<Departure> Departures, int TotalCount)> GetUpcomingDeparturesPageAsync(string? search, int page, int pageSize)
        {
            return await _departureRepository.GetPagedUpcomingAsync(search, page, pageSize);
        }

        public async Task<Departure?> GetDepartureByIdAsync(int id)
        {
            return await _departureRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<Departure> CreateDepartureAsync(Departure departure)
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                await EnsureNoBusScheduleConflictAsync(
                    departure.BusId,
                    departure.DepartureTime,
                    departure.ArrivalTime);

                var createdDeparture = await _departureRepository.CreateAsync(departure);

                var bus = await _departureRepository.GetBusByIdAsync(departure.BusId);
                if (bus != null)
                {
                    await _seatRepository.CreateSeatsForDepartureAsync(createdDeparture.Id, bus.Capacity);
                }

                return createdDeparture;
            });
        }

        public async Task<Departure> UpdateDepartureAsync(Departure departure)
        {
            await EnsureNoBusScheduleConflictAsync(
                departure.BusId,
                departure.DepartureTime,
                departure.ArrivalTime,
                departure.Id);

            return await _departureRepository.UpdateAsync(departure);
        }

        public async Task<Departure> UpdateDeparturePriceAsync(int departureId, decimal newPrice)
        {
            if (newPrice <= 0m)
                throw new InvalidOperationException("Price must be greater than 0.");

            return await ExecuteInTransactionAsync(async () =>
            {
                var departure = await _context.Departures
                    .FirstOrDefaultAsync(d => d.Id == departureId);

                if (departure == null)
                    throw new InvalidOperationException("Departure not found.");

                if (!departure.IsActive || departure.DepartureTime <= DateTime.UtcNow)
                {
                    throw new InvalidOperationException(
                        "Only prices of active and upcoming departures can be updated.");
                }

                departure.Price = NormalizePrice(newPrice);
                await _context.SaveChangesAsync();
                return departure;
            });
        }

        public async Task<int> BulkUpdateDeparturePricesAsync(
            int? routeId,
            int? busId,
            DateTime? startDate,
            DateTime? endDate,
            bool useMultiplier,
            decimal value)
        {
            if (value <= 0m)
                throw new InvalidOperationException("Update value must be greater than 0.");

            return await ExecuteInTransactionAsync(async () =>
            {
                var now = DateTime.UtcNow;
                var query = _context.Departures
                    .Where(d => d.IsActive && d.DepartureTime > now);

                if (routeId.HasValue && routeId.Value > 0)
                    query = query.Where(d => d.RouteId == routeId.Value);

                if (busId.HasValue && busId.Value > 0)
                    query = query.Where(d => d.BusId == busId.Value);

                if (startDate.HasValue)
                {
                    var start = startDate.Value.Date;
                    query = query.Where(d => d.DepartureTime >= start);
                }

                if (endDate.HasValue)
                {
                    var endExclusive = endDate.Value.Date.AddDays(1);
                    query = query.Where(d => d.DepartureTime < endExclusive);
                }

                var departures = await query.ToListAsync();
                if (!departures.Any())
                    return 0;

                foreach (var departure in departures)
                {
                    var computed = useMultiplier ? departure.Price * value : value;
                    departure.Price = NormalizePrice(computed);
                }

                await _context.SaveChangesAsync();
                return departures.Count;
            });
        }

        public async Task<bool> DeleteDepartureAsync(int id)
        {
            return await _departureRepository.DeleteAsync(id);
        }

        public async Task<bool> ToggleDepartureStatusAsync(int id)
        {
            var departure = await _departureRepository.GetByIdAsync(id);
            if (departure == null) return false;

            departure.IsActive = !departure.IsActive;
            await _departureRepository.UpdateAsync(departure);
            return true;
        }

        private async Task EnsureNoBusScheduleConflictAsync(
            int busId,
            DateTime departureTime,
            DateTime arrivalTime,
            int? excludeDepartureId = null)
        {
            var hasConflict = await _context.Departures
                .AsNoTracking()
                .Where(d => d.BusId == busId && d.IsActive)
                .Where(d => !excludeDepartureId.HasValue || d.Id != excludeDepartureId.Value)
                .AnyAsync(d => departureTime < d.ArrivalTime && arrivalTime > d.DepartureTime);

            if (hasConflict)
            {
                throw new InvalidOperationException(
                    "This bus is already assigned to another departure in the same time range.");
            }
        }

        private static decimal NormalizePrice(decimal rawPrice)
        {
            var normalized = Math.Round(rawPrice, 2, MidpointRounding.AwayFromZero);
            return normalized < 0.01m ? 0.01m : normalized;
        }

        private async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Dashboard Statistics
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var users = (await _userRepository.GetAllAsync()).ToList();
            var buses = (await _departureRepository.GetAllBusesAsync()).ToList();
            var routes = (await _departureRepository.GetAllRoutesAsync()).ToList();
            var departures = (await _departureRepository.GetAllAsync()).ToList();
            var tickets = (await _ticketRepository.GetAllAsync()).ToList();

            return new DashboardStats
            {
                TotalUsers = users.Count,
                TotalBuses = buses.Count,
                TotalRoutes = routes.Count,
                TotalDepartures = departures.Count,
                TotalTickets = tickets.Count,
                ActiveDepartures = departures.Count(d => d.IsActive && d.DepartureTime > DateTime.UtcNow),
                TotalRevenue = tickets
                    .Where(t => t.Payment != null && t.Payment.Status == PaymentStatus.Completed)
                    .Sum(t => t.Payment!.Amount),
                TodayTickets = tickets.Count(t => t.CreatedAt >= today && t.CreatedAt < tomorrow)
            };
        }
    }
}
