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
    }
}

