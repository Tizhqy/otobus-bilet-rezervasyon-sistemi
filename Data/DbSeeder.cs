using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon
{
    /// <summary>
    /// Veritabani ilk olusturulduğunda temel verileri ekler.
    /// Roller, ornek istasyonlar, rotalar, otobusler ve admin kullanicisi.
    /// </summary>
    public static class DbSeeder
    {
        private const int SeedDepartureDays = 7;
        private const int SeedTurnaroundMinutes = 75;
        private const int SeedMinimumRouteMinutes = 60;

        public static async Task SeedAsync(AppDbContext db)
        {
            // ── Roller ──────────────────────────────────────────────────────
            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Role { Name = "admin" },
                    new Role { Name = "user" }
                );
                await db.SaveChangesAsync();
            }

            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
            var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "user");

            // ── Admin Kullanicisi ────────────────────────────────────────────
            if (!await db.Users.AnyAsync(u => u.Email == "admin@hamsibus.com"))
            {
                db.Users.Add(new User
                {
                    FirstName = "Admin",
                    LastName = "HamsiBus",
                    Email = "admin@hamsibus.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Phone = "5551234567",
                    RoleId = adminRole!.Id,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }

            // ── Test Kullanicisi ─────────────────────────────────────────────
            if (!await db.Users.AnyAsync(u => u.Email == "test@hamsibus.com"))
            {
                db.Users.Add(new User
                {
                    FirstName = "Test",
                    LastName = "Kullanici",
                    Email = "test@hamsibus.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                    Phone = "5559876543",
                    RoleId = userRole!.Id,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }

            // ── Istasyonlar ─────────────────────────────────────────────────
            if (!await db.Stations.AnyAsync())
            {
                var stations = new[]
                {
                    new Station { Name = "Trabzon Otogar", City = "Trabzon", Address = "Trabzon Sehirlerarasi Otobus Terminali", IsActive = true },
                    new Station { Name = "Istanbul Esenler", City = "Istanbul", Address = "Esenler Otogar", IsActive = true },
                    new Station { Name = "Ankara ASTi", City = "Ankara", Address = "Ankara Sehirlerarasi Terminal Isletmesi", IsActive = true },
                    new Station { Name = "Izmir Otogar", City = "Izmir", Address = "Izmir Sehirlerarasi Otobus Terminali", IsActive = true },
                    new Station { Name = "Antalya Otogar", City = "Antalya", Address = "Antalya Sehirlerarasi Otobus Terminali", IsActive = true },
                    new Station { Name = "Bursa Otogar", City = "Bursa", Address = "Bursa Sehirlerarasi Otobus Terminali", IsActive = true },
                    new Station { Name = "Samsun Otogar", City = "Samsun", Address = "Samsun Sehirlerarasi Otobus Terminali", IsActive = true },
                    new Station { Name = "Rize Otogar", City = "Rize", Address = "Rize Sehirlerarasi Otobus Terminali", IsActive = true },
                };
                db.Stations.AddRange(stations);
                await db.SaveChangesAsync();
            }

            // ── Otobusler ───────────────────────────────────────────────────
            if (!await db.Buses.AnyAsync())
            {
                var buses = new[]
                {
                    new Bus { PlateNumber = "61 HB 001", Type = "2+1", Capacity = 42, IsActive = true },
                    new Bus { PlateNumber = "61 HB 002", Type = "2+2", Capacity = 48, IsActive = true },
                    new Bus { PlateNumber = "61 HB 003", Type = "2+1", Capacity = 42, IsActive = true },
                    new Bus { PlateNumber = "34 HB 004", Type = "2+2", Capacity = 48, IsActive = true },
                    new Bus { PlateNumber = "06 HB 005", Type = "1+1", Capacity = 24, IsActive = true },
                    new Bus { PlateNumber = "35 HB 006", Type = "2+1", Capacity = 33, IsActive = true },
                };
                db.Buses.AddRange(buses);
                await db.SaveChangesAsync();
            }

            // ── Rotalar ─────────────────────────────────────────────────────
            if (!await db.Routes.AnyAsync())
            {
                var trabzon = await db.Stations.FirstAsync(s => s.City == "Trabzon");
                var istanbul = await db.Stations.FirstAsync(s => s.City == "Istanbul");
                var ankara = await db.Stations.FirstAsync(s => s.City == "Ankara");
                var izmir = await db.Stations.FirstAsync(s => s.City == "Izmir");
                var antalya = await db.Stations.FirstAsync(s => s.City == "Antalya");
                var samsun = await db.Stations.FirstAsync(s => s.City == "Samsun");

                var routes = new[]
                {
                    new Route { OriginStationId = trabzon.Id, DestinationStationId = istanbul.Id, DistanceKm = 1070, DurationMinutes = 780, IsActive = true },
                    new Route { OriginStationId = istanbul.Id, DestinationStationId = trabzon.Id, DistanceKm = 1070, DurationMinutes = 780, IsActive = true },
                    new Route { OriginStationId = trabzon.Id, DestinationStationId = ankara.Id, DistanceKm = 780, DurationMinutes = 600, IsActive = true },
                    new Route { OriginStationId = ankara.Id, DestinationStationId = istanbul.Id, DistanceKm = 450, DurationMinutes = 360, IsActive = true },
                    new Route { OriginStationId = istanbul.Id, DestinationStationId = izmir.Id, DistanceKm = 480, DurationMinutes = 330, IsActive = true },
                    new Route { OriginStationId = ankara.Id, DestinationStationId = antalya.Id, DistanceKm = 550, DurationMinutes = 420, IsActive = true },
                    new Route { OriginStationId = trabzon.Id, DestinationStationId = samsun.Id, DistanceKm = 340, DurationMinutes = 240, IsActive = true },
                };
                db.Routes.AddRange(routes);
                await db.SaveChangesAsync();
            }

            // ── Seferler + Koltuklar ────────────────────────────────────────
            await EnsureDeparturesAndSeatsAsync(db);
        }

        private static async Task EnsureDeparturesAndSeatsAsync(AppDbContext db)
        {
            var routes = await db.Routes
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Id)
                .ToListAsync();
            var buses = await db.Buses
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .ToListAsync();

            if (!routes.Any() || !buses.Any())
                return;

            var now = DateTime.UtcNow;

            var existingDepartures = await db.Departures
                .AsNoTracking()
                .Select(d => new DepartureScheduleEntry(d.BusId, d.DepartureTime, d.ArrivalTime))
                .ToListAsync();

            var shouldRegenerate = !existingDepartures.Any();

            if (!shouldRegenerate)
            {
                var hasTickets = await db.Tickets.AnyAsync();
                if (!hasTickets && HasScheduleConflicts(existingDepartures))
                {
                    shouldRegenerate = true;
                }
            }

            if (shouldRegenerate)
            {
                if (await db.Departures.AnyAsync())
                {
                    db.Seats.RemoveRange(db.Seats);
                    db.Departures.RemoveRange(db.Departures);
                    await db.SaveChangesAsync();
                }

                var seedStartDate = DateTime.UtcNow.Date.AddDays(1);
                var departures = GenerateDepartures(routes, buses, seedStartDate, SeedDepartureDays);
                db.Departures.AddRange(departures);
                await db.SaveChangesAsync();
            }
            else
            {
                await EnsureUpcomingDepartureHorizonAsync(db, routes, buses, now);
            }

            await EnsureSeatsCreatedAsync(db);
        }

        private static async Task EnsureUpcomingDepartureHorizonAsync(
            AppDbContext db,
            IReadOnlyList<Route> routes,
            IReadOnlyList<Bus> buses,
            DateTime nowUtc)
        {
            var latestUpcomingArrival = await db.Departures
                .AsNoTracking()
                .Where(d => d.IsActive && d.ArrivalTime > nowUtc)
                .MaxAsync(d => (DateTime?)d.ArrivalTime);

            var desiredLastDate = nowUtc.Date.AddDays(SeedDepartureDays);
            var startDate = (latestUpcomingArrival?.Date ?? nowUtc.Date).AddDays(1);
            var daysToGenerate = (desiredLastDate - startDate).Days + 1;

            if (daysToGenerate <= 0)
                return;

            var departures = GenerateDepartures(routes, buses, startDate, daysToGenerate);
            db.Departures.AddRange(departures);
            await db.SaveChangesAsync();
        }

        private static List<Departure> GenerateDepartures(
            IReadOnlyList<Route> routes,
            IReadOnlyList<Bus> buses,
            DateTime seedStartDateUtc,
            int dayCount)
        {
            var departures = new List<Departure>();
            var busNextAvailableUtc = buses.ToDictionary(
                bus => bus.Id,
                _ => seedStartDateUtc.AddHours(6));

            for (int dayOffset = 0; dayOffset < dayCount; dayOffset++)
            {
                var dayStart = seedStartDateUtc.AddDays(dayOffset);

                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    var route = routes[routeIndex];
                    var preferredDeparture = dayStart.AddHours(7 + ((routeIndex * 2) % 12));
                    var durationMinutes = Math.Max(route.DurationMinutes ?? 0, SeedMinimumRouteMinutes);
                    var distanceKm = Math.Max(route.DistanceKm ?? 0, 0);

                    var selectedBus = buses
                        .Select(bus => new
                        {
                            Bus = bus,
                            EarliestDeparture = busNextAvailableUtc[bus.Id] > preferredDeparture
                                ? busNextAvailableUtc[bus.Id]
                                : preferredDeparture
                        })
                        .OrderBy(x => x.EarliestDeparture)
                        .ThenBy(x => x.Bus.Id)
                        .First();

                    var departureTime = selectedBus.EarliestDeparture;
                    var arrivalTime = departureTime.AddMinutes(durationMinutes);

                    departures.Add(new Departure
                    {
                        RouteId = route.Id,
                        BusId = selectedBus.Bus.Id,
                        DepartureTime = departureTime,
                        ArrivalTime = arrivalTime,
                        Price = 200m + (distanceKm / 10m) * 2m,
                        IsActive = true
                    });

                    busNextAvailableUtc[selectedBus.Bus.Id] = arrivalTime.AddMinutes(SeedTurnaroundMinutes);
                }
            }

            return departures;
        }

        private static bool HasScheduleConflicts(IEnumerable<DepartureScheduleEntry> departures)
        {
            foreach (var group in departures.GroupBy(d => d.BusId))
            {
                var ordered = group
                    .OrderBy(d => d.DepartureTime)
                    .ToList();

                for (int i = 1; i < ordered.Count; i++)
                {
                    var previous = ordered[i - 1];
                    var current = ordered[i];
                    if (current.DepartureTime < previous.ArrivalTime.AddMinutes(SeedTurnaroundMinutes))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static async Task EnsureSeatsCreatedAsync(AppDbContext db)
        {
            var departuresWithoutSeats = await db.Departures
                .AsNoTracking()
                .Where(d => !db.Seats.Any(s => s.DepartureId == d.Id))
                .Select(d => new { d.Id, d.BusId })
                .ToListAsync();

            if (!departuresWithoutSeats.Any())
                return;

            var busCapacities = await db.Buses
                .AsNoTracking()
                .ToDictionaryAsync(b => b.Id, b => b.Capacity);

            var seats = new List<Seat>();
            foreach (var dep in departuresWithoutSeats)
            {
                if (!busCapacities.TryGetValue(dep.BusId, out var capacity) || capacity <= 0)
                    continue;

                for (int seatNum = 1; seatNum <= capacity; seatNum++)
                {
                    seats.Add(new Seat
                    {
                        DepartureId = dep.Id,
                        SeatNumber = seatNum.ToString(),
                        Status = SeatStatus.Available
                    });
                }
            }

            db.Seats.AddRange(seats);
            await db.SaveChangesAsync();
        }

        private sealed record DepartureScheduleEntry(int BusId, DateTime DepartureTime, DateTime ArrivalTime);
    }
}
