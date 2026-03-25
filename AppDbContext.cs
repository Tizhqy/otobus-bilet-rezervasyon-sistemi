using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteStation> RouteStations { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Departure> Departures { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Role configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.Name).IsUnique();
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Station configuration - no additional config needed

            // Route configuration
            modelBuilder.Entity<Route>(entity =>
            {
                entity.HasOne(r => r.OriginStation)
                    .WithMany(s => s.OriginRoutes)
                    .HasForeignKey(r => r.OriginStationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.DestinationStation)
                    .WithMany(s => s.DestinationRoutes)
                    .HasForeignKey(r => r.DestinationStationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // RouteStation configuration
            modelBuilder.Entity<RouteStation>(entity =>
            {
                entity.HasIndex(rs => new { rs.RouteId, rs.StopOrder }).IsUnique();

                entity.HasOne(rs => rs.Route)
                    .WithMany(r => r.RouteStations)
                    .HasForeignKey(rs => rs.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Station)
                    .WithMany(s => s.RouteStations)
                    .HasForeignKey(rs => rs.StationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Bus configuration
            modelBuilder.Entity<Bus>(entity =>
            {
                entity.HasIndex(b => b.PlateNumber).IsUnique();
            });

            // Departure configuration
            modelBuilder.Entity<Departure>(entity =>
            {
                entity.HasOne(d => d.Route)
                    .WithMany(r => r.Departures)
                    .HasForeignKey(d => d.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Bus)
                    .WithMany(b => b.Departures)
                    .HasForeignKey(d => d.BusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seat configuration
            modelBuilder.Entity<Seat>(entity =>
            {
                entity.HasIndex(s => new { s.DepartureId, s.SeatNumber }).IsUnique();

                entity.Property(s => s.Status)
                    .HasConversion<string>();

                entity.HasOne(s => s.Departure)
                    .WithMany(d => d.Seats)
                    .HasForeignKey(s => s.DepartureId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Ticket configuration
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.Property(t => t.Status)
                    .HasConversion<string>();

                entity.HasOne(t => t.User)
                    .WithMany(u => u.Tickets)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Departure)
                    .WithMany(d => d.Tickets)
                    .HasForeignKey(t => t.DepartureId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Passenger configuration
            modelBuilder.Entity<Passenger>(entity =>
            {
                entity.HasIndex(p => p.SeatId).IsUnique();

                entity.HasOne(p => p.Ticket)
                    .WithMany(t => t.Passengers)
                    .HasForeignKey(p => p.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Seat)
                    .WithOne(s => s.Passenger)
                    .HasForeignKey<Passenger>(p => p.SeatId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Method)
                    .HasConversion<string>();

                entity.Property(p => p.Status)
                    .HasConversion<string>();

                entity.HasOne(p => p.Ticket)
                    .WithOne(t => t.Payment)
                    .HasForeignKey<Payment>(p => p.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Log configuration
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasOne(l => l.User)
                    .WithMany(u => u.Logs)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // PasswordReset configuration
            modelBuilder.Entity<PasswordReset>(entity =>
            {
                entity.HasOne(pr => pr.User)
                    .WithMany(u => u.PasswordResets)
                    .HasForeignKey(pr => pr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
