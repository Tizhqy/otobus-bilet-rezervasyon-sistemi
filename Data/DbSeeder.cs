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
                    new Bus { PlateNumber = "61 HB 001", Type = "2+1", Capacity = 40, IsActive = true },
                    new Bus { PlateNumber = "61 HB 002", Type = "2+2", Capacity = 46, IsActive = true },
                    new Bus { PlateNumber = "61 HB 003", Type = "2+1", Capacity = 40, IsActive = true },
                    new Bus { PlateNumber = "34 HB 004", Type = "2+2", Capacity = 46, IsActive = true },
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

            // ── Seferler ────────────────────────────────────────────────────
            if (!await db.Departures.AnyAsync())
            {
                var routes = await db.Routes.ToListAsync();
                var buses = await db.Buses.ToListAsync();
                var departures = new List<Departure>();

                // Yaklasik 1 haftalik seferler olustur
                for (int dayOffset = 0; dayOffset < 7; dayOffset++)
                {
                    var date = DateTime.Now.Date.AddDays(dayOffset + 1);

                    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                    {
                        var route = routes[routeIndex];
                        var bus = buses[routeIndex % buses.Count];
                        var departureTime = date.AddHours(8 + (routeIndex * 3) % 14); // Farkli saatler
                        var durationMinutes = route.DurationMinutes ?? 0;
                        var distanceKm = route.DistanceKm ?? 0;

                        departures.Add(new Departure
                        {
                            RouteId = route.Id,
                            BusId = bus.Id,
                            DepartureTime = departureTime,
                            ArrivalTime = departureTime.AddMinutes(durationMinutes),
                            Price = 200m + (distanceKm / 10m) * 2m,
                            IsActive = true
                        });
                    }
                }

                db.Departures.AddRange(departures);
                await db.SaveChangesAsync();

                // Her sefer icin koltuk olustur
                var allDepartures = await db.Departures.Include(d => d.Bus).ToListAsync();
                foreach (var dep in allDepartures)
                {
                    for (int seatNum = 1; seatNum <= dep.Bus.Capacity; seatNum++)
                    {
                        db.Seats.Add(new Seat
                        {
                            DepartureId = dep.Id,
                            SeatNumber = seatNum.ToString(),
                            Status = SeatStatus.Available
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
