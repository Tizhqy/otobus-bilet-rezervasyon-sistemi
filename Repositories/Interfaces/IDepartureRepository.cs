using OtobusBiletRezervasyon.Models;
using Route = OtobusBiletRezervasyon.Models.Route;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface IDepartureRepository
    {
        Task<Departure?> GetByIdAsync(int id);
        Task<Departure?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Departure>> GetAllAsync();
        Task<IEnumerable<Departure>> GetActiveAsync();
        Task<Departure> CreateAsync(Departure departure);
        Task<Departure> UpdateAsync(Departure departure);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Search
        Task<IEnumerable<Departure>> SearchAsync(int originStationId, int destinationStationId, DateTime date);
        Task<IEnumerable<Departure>> GetByRouteIdAsync(int routeId);
        Task<IEnumerable<Departure>> GetByBusIdAsync(int busId);
        Task<IEnumerable<Departure>> GetUpcomingAsync(int count = 10);

        // Route
        Task<Route?> GetRouteByIdAsync(int id);
        Task<IEnumerable<Route>> GetAllRoutesAsync();
        Task<IEnumerable<Route>> GetActiveRoutesAsync();
        Task<Route?> GetRouteByStationsAsync(int originStationId, int destinationStationId);
        Task<Route> CreateRouteAsync(Route route);
        Task<Route> UpdateRouteAsync(Route route);
        Task<bool> DeleteRouteAsync(int id);

        // Station
        Task<Station?> GetStationByIdAsync(int id);
        Task<IEnumerable<Station>> GetAllStationsAsync();
        Task<IEnumerable<Station>> GetActiveStationsAsync();
        Task<Station> CreateStationAsync(Station station);
        Task<Station> UpdateStationAsync(Station station);
        Task<bool> DeleteStationAsync(int id);

        // Bus
        Task<Bus?> GetBusByIdAsync(int id);
        Task<IEnumerable<Bus>> GetAllBusesAsync();
        Task<IEnumerable<Bus>> GetActiveBusesAsync();
        Task<Bus> CreateBusAsync(Bus bus);
        Task<Bus> UpdateBusAsync(Bus bus);
        Task<bool> DeleteBusAsync(int id);
    }
}
