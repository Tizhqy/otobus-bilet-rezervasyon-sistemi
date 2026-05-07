using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class AdminDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<Log> RecentLogs { get; set; } = new();
        public List<Bus> Buses { get; set; } = new();
        public List<Route> Routes { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Departure> UpcomingDepartures { get; set; } = new();
        public List<Route> RouteOptions { get; set; } = new();
        public List<Bus> BusOptions { get; set; } = new();

        // Pagination for Upcoming Departures
        public int DeparturesTotalCount { get; set; }
        public int DeparturesCurrentPage { get; set; } = 1;
        public int DeparturesTotalPages { get; set; } = 1;
        public string? DepartureSearchTerm { get; set; }
    }
}

