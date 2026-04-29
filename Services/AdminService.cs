using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;
using Route = OtobusBiletRezervasyon.Models.Route;

namespace OtobusBiletRezervasyon.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly IDepartureRepository _departureRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly AppDbContext _context;

        public AdminService(
            IUserRepository userRepository,
            IDepartureRepository departureRepository,
            ISeatRepository seatRepository,
            ITicketRepository ticketRepository,
            AppDbContext context)
        {
            _userRepository = userRepository;
            _departureRepository = departureRepository;
            _seatRepository = seatRepository;
            _ticketRepository = ticketRepository;
            _context = context;
        }

        // User Management
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
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

        public async Task<Departure?> GetDepartureByIdAsync(int id)
        {
            return await _departureRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<Departure> CreateDepartureAsync(Departure departure)
        {
            var createdDeparture = await _departureRepository.CreateAsync(departure);

            // Create seats for the departure based on bus capacity
            var bus = await _departureRepository.GetBusByIdAsync(departure.BusId);
            if (bus != null)
            {
                await _seatRepository.CreateSeatsForDepartureAsync(createdDeparture.Id, bus.Capacity);
            }

            return createdDeparture;
        }

        public async Task<Departure> UpdateDepartureAsync(Departure departure)
        {
            return await _departureRepository.UpdateAsync(departure);
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

        // Dashboard Statistics
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return new DashboardStats
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalBuses = await _context.Buses.CountAsync(),
                TotalRoutes = await _context.Routes.CountAsync(),
                TotalDepartures = await _context.Departures.CountAsync(),
                TotalTickets = await _context.Tickets.CountAsync(),
                ActiveDepartures = await _context.Departures
                    .CountAsync(d => d.IsActive && d.DepartureTime > DateTime.Now),
                TotalRevenue = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .SumAsync(p => p.Amount),
                TodayTickets = await _context.Tickets
                    .CountAsync(t => t.CreatedAt >= today && t.CreatedAt < tomorrow)
            };
        }
    }
}
