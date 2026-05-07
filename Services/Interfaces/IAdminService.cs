using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IAdminService
    {
        // User Management
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<(IReadOnlyList<User> Users, int TotalCount)> GetUsersPageAsync(string? search, int page, int pageSize);
        Task<User?> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(User user, string password);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ToggleUserStatusAsync(int id);
        Task<bool> RoleExistsAsync(int roleId);

        // Bus Management
        Task<IEnumerable<Bus>> GetAllBusesAsync();
        Task<Bus?> GetBusByIdAsync(int id);
        Task<Bus> CreateBusAsync(Bus bus);
        Task<Bus> UpdateBusAsync(Bus bus);
        Task<bool> DeleteBusAsync(int id);
        Task<bool> ToggleBusStatusAsync(int id);

        // Route Management
        Task<IEnumerable<Route>> GetAllRoutesAsync();
        Task<Route?> GetRouteByIdAsync(int id);
        Task<Route> CreateRouteAsync(Route route);
        Task<Route> UpdateRouteAsync(Route route);
        Task<bool> DeleteRouteAsync(int id);
        Task<bool> ToggleRouteStatusAsync(int id);

        // Station Management
        Task<IEnumerable<Station>> GetAllStationsAsync();
        Task<Station?> GetStationByIdAsync(int id);
        Task<Station> CreateStationAsync(Station station);
        Task<Station> UpdateStationAsync(Station station);
        Task<bool> DeleteStationAsync(int id);
        Task<bool> ToggleStationStatusAsync(int id);

        // Departure Management
        Task<IEnumerable<Departure>> GetAllDeparturesAsync();
        Task<IEnumerable<Departure>> GetUpcomingDeparturesAsync(int count = 100);
        Task<(IReadOnlyList<Departure> Departures, int TotalCount)> GetUpcomingDeparturesPageAsync(string? search, int page, int pageSize);
        Task<Departure?> GetDepartureByIdAsync(int id);
        Task<Departure> CreateDepartureAsync(Departure departure);
        Task<Departure> UpdateDepartureAsync(Departure departure);
        Task<Departure> UpdateDeparturePriceAsync(int departureId, decimal newPrice);
        Task<int> BulkUpdateDeparturePricesAsync(
            int? routeId,
            int? busId,
            DateTime? startDate,
            DateTime? endDate,
            bool useMultiplier,
            decimal value);
        Task<bool> DeleteDepartureAsync(int id);
        Task<bool> ToggleDepartureStatusAsync(int id);

        // Dashboard Statistics
        Task<DashboardStats> GetDashboardStatsAsync();
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalBuses { get; set; }
        public int TotalRoutes { get; set; }
        public int TotalDepartures { get; set; }
        public int TotalTickets { get; set; }
        public int ActiveDepartures { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TodayTickets { get; set; }
    }
}
