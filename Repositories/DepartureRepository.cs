using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Repositories
{
    public class DepartureRepository : IDepartureRepository
    {
        private readonly AppDbContext _context;

        public DepartureRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Departure?> GetByIdAsync(int id)
        {
            return await _context.Departures.FindAsync(id);
        }

        public async Task<Departure?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Departures
                .Include(d => d.Route)
                    .ThenInclude(r => r.OriginStation)
                .Include(d => d.Route)
                    .ThenInclude(r => r.DestinationStation)
                .Include(d => d.Bus)
                .Include(d => d.Seats)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Departure>> GetAllAsync()
        {
            return await _context.Departures
                .Include(d => d.Route)
                    .ThenInclude(r => r.OriginStation)
                .Include(d => d.Route)
                    .ThenInclude(r => r.DestinationStation)
                .Include(d => d.Bus)
                .OrderBy(d => d.DepartureTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Departure>> GetActiveAsync()
        {
            return await _context.Departures
                .Include(d => d.Route)
                    .ThenInclude(r => r.OriginStation)
                .Include(d => d.Route)
                    .ThenInclude(r => r.DestinationStation)
                .Include(d => d.Bus)
                .Where(d => d.IsActive && d.DepartureTime > DateTime.Now)
                .OrderBy(d => d.DepartureTime)
                .ToListAsync();
        }

        public async Task<Departure> CreateAsync(Departure departure)
        {
            _context.Departures.Add(departure);
            await _context.SaveChangesAsync();
            return departure;
        }

        public async Task<Departure> UpdateAsync(Departure departure)
        {
            _context.Departures.Update(departure);
            await _context.SaveChangesAsync();
            return departure;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var departure = await _context.Departures.FindAsync(id);
            if (departure == null) return false;

            _context.Departures.Remove(departure);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Departures.AnyAsync(d => d.Id == id);
        }

        // Search
        public async Task<IEnumerable<Departure>> SearchAsync(int originStationId, int destinationStationId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1);

            return await _context.Departures
                .Include(d => d.Route)
                    .ThenInclude(r => r.OriginStation)
                .Include(d => d.Route)
                    .ThenInclude(r => r.DestinationStation)
                .Include(d => d.Bus)
                .Include(d => d.Seats)
                .Where(d => d.IsActive
                    && d.Route.OriginStationId == originStationId
                    && d.Route.DestinationStationId == destinationStationId
                    && d.DepartureTime >= startOfDay
                    && d.DepartureTime < endOfDay
                    && d.DepartureTime > DateTime.Now)
                .OrderBy(d => d.DepartureTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Departure>> GetByRouteIdAsync(int routeId)
        {
            return await _context.Departures
                .Include(d => d.Bus)
                .Where(d => d.RouteId == routeId)
                .OrderBy(d => d.DepartureTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Departure>> GetByBusIdAsync(int busId)
        {
            return await _context.Departures
                .Include(d => d.Route)
                .Where(d => d.BusId == busId)
                .OrderBy(d => d.DepartureTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Departure>> GetUpcomingAsync(int count = 10)
        {
            return await _context.Departures
                .Include(d => d.Route)
                    .ThenInclude(r => r.OriginStation)
                .Include(d => d.Route)
                    .ThenInclude(r => r.DestinationStation)
                .Include(d => d.Bus)
                .Where(d => d.IsActive && d.DepartureTime > DateTime.Now)
                .OrderBy(d => d.DepartureTime)
                .Take(count)
                .ToListAsync();
        }

        // Route
        public async Task<Route?> GetRouteByIdAsync(int id)
        {
            return await _context.Routes
                .Include(r => r.OriginStation)
                .Include(r => r.DestinationStation)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Route>> GetAllRoutesAsync()
        {
            return await _context.Routes
                .Include(r => r.OriginStation)
                .Include(r => r.DestinationStation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Route>> GetActiveRoutesAsync()
        {
            return await _context.Routes
                .Include(r => r.OriginStation)
                .Include(r => r.DestinationStation)
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<Route?> GetRouteByStationsAsync(int originStationId, int destinationStationId)
        {
            return await _context.Routes
                .Include(r => r.OriginStation)
                .Include(r => r.DestinationStation)
                .FirstOrDefaultAsync(r => r.OriginStationId == originStationId
                    && r.DestinationStationId == destinationStationId);
        }

        public async Task<Route> CreateRouteAsync(Route route)
        {
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();
            return route;
        }

        public async Task<Route> UpdateRouteAsync(Route route)
        {
            _context.Routes.Update(route);
            await _context.SaveChangesAsync();
            return route;
        }

        public async Task<bool> DeleteRouteAsync(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null) return false;

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();
            return true;
        }

        // Station
        public async Task<Station?> GetStationByIdAsync(int id)
        {
            return await _context.Stations.FindAsync(id);
        }

        public async Task<IEnumerable<Station>> GetAllStationsAsync()
        {
            return await _context.Stations.ToListAsync();
        }

        public async Task<IEnumerable<Station>> GetActiveStationsAsync()
        {
            return await _context.Stations.Where(s => s.IsActive).ToListAsync();
        }

        public async Task<Station> CreateStationAsync(Station station)
        {
            _context.Stations.Add(station);
            await _context.SaveChangesAsync();
            return station;
        }

        public async Task<Station> UpdateStationAsync(Station station)
        {
            _context.Stations.Update(station);
            await _context.SaveChangesAsync();
            return station;
        }

        public async Task<bool> DeleteStationAsync(int id)
        {
            var station = await _context.Stations.FindAsync(id);
            if (station == null) return false;

            _context.Stations.Remove(station);
            await _context.SaveChangesAsync();
            return true;
        }

        // Bus
        public async Task<Bus?> GetBusByIdAsync(int id)
        {
            return await _context.Buses.FindAsync(id);
        }

        public async Task<IEnumerable<Bus>> GetAllBusesAsync()
        {
            return await _context.Buses.ToListAsync();
        }

        public async Task<IEnumerable<Bus>> GetActiveBusesAsync()
        {
            return await _context.Buses.Where(b => b.IsActive).ToListAsync();
        }

        public async Task<Bus> CreateBusAsync(Bus bus)
        {
            _context.Buses.Add(bus);
            await _context.SaveChangesAsync();
            return bus;
        }

        public async Task<Bus> UpdateBusAsync(Bus bus)
        {
            _context.Buses.Update(bus);
            await _context.SaveChangesAsync();
            return bus;
        }

        public async Task<bool> DeleteBusAsync(int id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return false;

            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
