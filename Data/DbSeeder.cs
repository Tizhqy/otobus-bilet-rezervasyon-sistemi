using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon
{
    /// <summary>
    /// Adds initial seed data when the database is first created.
    /// Roles, example stations, routes, buses, and an admin user.
    /// </summary>
    public static class DbSeeder
    {
        private const int SeedDepartureDays = 7;
        private const int SeedTurnaroundMinutes = 75;
        private const int SeedMinimumRouteMinutes = 60;

        public static async Task SeedAsync(AppDbContext db)
        {
            // ── Roles ──────────────────────────────────────────────────────
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

            // ── Admin User ────────────────────────────────────────────
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

            // ── Test User ─────────────────────────────────────────────
            if (!await db.Users.AnyAsync(u => u.Email == "test@hamsibus.com"))
            {
                db.Users.Add(new User
                {
                    FirstName = "Test",
                    LastName = "User",
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
                    new Bus { PlateNumber = "35 HB 007", Type = "2+2", Capacity = 48, IsActive = true },
                    new Bus { PlateNumber = "16 HB 008", Type = "2+1", Capacity = 42, IsActive = true },
                    new Bus { PlateNumber = "42 HB 009", Type = "2+2", Capacity = 48, IsActive = true },
                    new Bus { PlateNumber = "26 HB 010", Type = "2+1", Capacity = 42, IsActive = true },
                    new Bus { PlateNumber = "06 HB 011", Type = "1+1", Capacity = 24, IsActive = true },
                    new Bus { PlateNumber = "41 HB 012", Type = "2+1", Capacity = 33, IsActive = true },
                    // Eklenen yeni otobusler
                    new Bus { PlateNumber = "55 HB 013", Type = "2+2", Capacity = 48, IsActive = true },
                    new Bus { PlateNumber = "61 HB 014", Type = "2+1", Capacity = 42, IsActive = true },
                    new Bus { PlateNumber = "34 HB 015", Type = "1+1", Capacity = 24, IsActive = true },
                    new Bus { PlateNumber = "06 HB 016", Type = "2+1", Capacity = 33, IsActive = true },
                    new Bus { PlateNumber = "07 HB 017", Type = "2+2", Capacity = 48, IsActive = true },
                    new Bus { PlateNumber = "53 HB 018", Type = "2+1", Capacity = 42, IsActive = true },
                    new Bus { PlateNumber = "16 HB 019", Type = "2+2", Capacity = 48, IsActive = true },
                    new Bus { PlateNumber = "35 HB 020", Type = "2+1", Capacity = 33, IsActive = true },
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
                var bursa = await db.Stations.FirstAsync(s => s.City == "Bursa");
                var samsun = await db.Stations.FirstAsync(s => s.City == "Samsun");
                var rize = await db.Stations.FirstAsync(s => s.City == "Rize");

                var routePairs = new[]
                {
                    (trabzon, istanbul, 1070, 780),
                    (trabzon, ankara, 780, 600),
                    (ankara, istanbul, 450, 360),
                    (istanbul, izmir, 480, 330),
                    (ankara, antalya, 550, 420),
                    (trabzon, samsun, 340, 240),
                    (rize, trabzon, 80, 90),
                    (istanbul, bursa, 150, 120),
                    (bursa, izmir, 330, 240),
                    (ankara, bursa, 390, 300)
                };

                var routes = new List<Route>();
                foreach (var (origin, dest, dist, dur) in routePairs)
                {
                    // Cift yonlu rota ekleme
                    routes.Add(new Route { OriginStationId = origin.Id, DestinationStationId = dest.Id, DistanceKm = dist, DurationMinutes = dur, IsActive = true });
                    routes.Add(new Route { OriginStationId = dest.Id, DestinationStationId = origin.Id, DistanceKm = dist, DurationMinutes = dur, IsActive = true });
                }

                db.Routes.AddRange(routes);
                await db.SaveChangesAsync();
            }

            // ── Seferler + Koltuklar ────────────────────────────────────────
            await EnsureDeparturesAndSeatsAsync(db);
            
            // ── Kuponlar ───────────────────────────────────────────────
            await EnsureCouponsAsync(db);
        }

        private static async Task EnsureCouponsAsync(AppDbContext db)
        {
            if (!await db.Coupons.AnyAsync())
            {
                var coupons = new[]
                {
                    new Coupon { Code = "HAMSIDEV20", DiscountAmount = 20, DiscountType = "Percentage", IsActive = true, ValidUntil = DateTime.UtcNow.AddMonths(1) },
                    new Coupon { Code = "OGRENCI10", DiscountAmount = 10, DiscountType = "Percentage", IsActive = true, ValidUntil = DateTime.UtcNow.AddMonths(1) },
                    new Coupon { Code = "SABIT50", DiscountAmount = 50, DiscountType = "Fixed", IsActive = true, ValidUntil = DateTime.UtcNow.AddMonths(1) },
                    new Coupon { Code = "HOSGELDIN100", DiscountAmount = 100, DiscountType = "Fixed", IsActive = true, ValidUntil = DateTime.UtcNow.AddMonths(1) }
                };
                db.Coupons.AddRange(coupons);
                await db.SaveChangesAsync();
            }
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

            // Force regeneration if bus count changed (e.g., added more buses)
            var expectedDeparturesPerDay = routes.Count * buses.Count;
            var actualDeparturesPerDay = existingDepartures.GroupBy(d => d.DepartureTime.Date).FirstOrDefault()?.Count() ?? 0;
            
            var shouldRegenerate = !existingDepartures.Any() || 
                (actualDeparturesPerDay > 0 && actualDeparturesPerDay < expectedDeparturesPerDay / 2);

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
                // If there are any existing tickets, do NOT force-delete departures/seats
                // because passengers/tickets have FKs to seats (ON DELETE RESTRICT).
                var hasTickets = await db.Tickets.AnyAsync();
                if (hasTickets)
                {
                    // Skip regeneration to avoid FK constraint violations in a live DB.
                    // EnsureUpcomingDepartureHorizonAsync will still keep horizon up-to-date.
                    await EnsureUpcomingDepartureHorizonAsync(db, routes, buses, now);
                }
                else
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
            }
            else
            {
                await EnsureUpcomingDepartureHorizonAsync(db, routes, buses, now);
            }

            await EnsureEachBusHasDepartureAsync(db, routes, buses, now);
            await NormalizeUpcomingDeparturePricesAsync(db, routes, now);
            await EnsureSeatsCreatedAsync(db);
        }

        private static async Task EnsureEachBusHasDepartureAsync(
            AppDbContext db,
            IReadOnlyList<Route> routes,
            IReadOnlyList<Bus> buses,
            DateTime nowUtc)
        {
            if (routes.Count == 0 || buses.Count == 0)
                return;

            var busesWithUpcoming = await db.Departures
                .AsNoTracking()
                .Where(d => d.IsActive && d.DepartureTime > nowUtc)
                .Select(d => d.BusId)
                .Distinct()
                .ToListAsync();

            var missingBusIds = buses
                .Select(b => b.Id)
                .Except(busesWithUpcoming)
                .ToList();

            if (missingBusIds.Count == 0)
                return;

            var baseDepartureTime = nowUtc.Date.AddDays(1).AddHours(7);

            var additionalDepartures = new List<Departure>();
            for (int i = 0; i < missingBusIds.Count; i++)
            {
                var route = routes[i % routes.Count];
                var durationMinutes = Math.Max(route.DurationMinutes ?? 0, SeedMinimumRouteMinutes);
                var distanceKm = Math.Max(route.DistanceKm ?? 0, 0);
                var basePrice = 50m + (distanceKm / 10m) * 1.5m;
                var departureTime = baseDepartureTime.AddMinutes(20 * i);
                var arrivalTime = departureTime.AddMinutes(durationMinutes);

                additionalDepartures.Add(new Departure
                {
                    RouteId = route.Id,
                    BusId = missingBusIds[i],
                    DepartureTime = departureTime,
                    ArrivalTime = arrivalTime,
                    Price = basePrice,
                    IsActive = true
                });
            }

            db.Departures.AddRange(additionalDepartures);
            await db.SaveChangesAsync();
        }

        private static async Task NormalizeUpcomingDeparturePricesAsync(
            AppDbContext db,
            IReadOnlyList<Route> routes,
            DateTime nowUtc)
        {
            var upcomingDepartures = await db.Departures
                .AsNoTracking()
                .Where(d => d.IsActive && d.DepartureTime > nowUtc)
                .Select(d => new { d.Id, d.RouteId, d.Price })
                .ToListAsync();

            if (!upcomingDepartures.Any())
                return;

            var routeLookup = routes.ToDictionary(r => r.Id);
            var updates = new List<Departure>();

            foreach (var departure in upcomingDepartures)
            {
                if (!routeLookup.TryGetValue(departure.RouteId, out var route))
                    continue;

                var distanceKm = Math.Max(route.DistanceKm ?? 0, 0);
                var targetPrice = 50m + (distanceKm / 10m) * 1.5m;

                if (departure.Price > targetPrice * 1.25m)
                {
                    updates.Add(new Departure { Id = departure.Id, Price = targetPrice });
                }
            }

            if (updates.Count == 0)
                return;

            foreach (var update in updates)
            {
                db.Departures.Attach(update);
                db.Entry(update).Property(d => d.Price).IsModified = true;
            }

            await db.SaveChangesAsync();
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

                for (int busIndex = 0; busIndex < buses.Count; busIndex++)
                {
                    var bus = buses[busIndex];
                    var route = routes[(busIndex + dayOffset) % routes.Count];
                    
                    var preferredDeparture = dayStart.AddHours(7 + ((busIndex * 2) % 12));
                    var durationMinutes = Math.Max(route.DurationMinutes ?? 0, SeedMinimumRouteMinutes);
                    var distanceKm = Math.Max(route.DistanceKm ?? 0, 0);

                    var departureTime = busNextAvailableUtc[bus.Id] > preferredDeparture
                        ? busNextAvailableUtc[bus.Id]
                        : preferredDeparture;
                        
                    var arrivalTime = departureTime.AddMinutes(durationMinutes);

                    departures.Add(new Departure
                    {
                        RouteId = route.Id,
                        BusId = bus.Id,
                        DepartureTime = departureTime,
                        ArrivalTime = arrivalTime,
                        Price = 50m + (distanceKm / 10m) * 1.5m,
                        IsActive = true
                    });

                    busNextAvailableUtc[bus.Id] = arrivalTime.AddMinutes(SeedTurnaroundMinutes);
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
