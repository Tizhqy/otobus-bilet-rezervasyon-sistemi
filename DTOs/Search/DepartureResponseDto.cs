namespace OtobusBiletRezervasyon.DTOs.Search
{
    public class DepartureResponseDto
    {
        public int Id { get; set; }
        public RouteInfoDto Route { get; set; } = null!;
        public BusInfoDto Bus { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
        public bool IsDynamicPricingApplied { get; set; }
    }

    public class RouteInfoDto
    {
        public int Id { get; set; }
        public StationInfoDto OriginStation { get; set; } = null!;
        public StationInfoDto DestinationStation { get; set; } = null!;
        public int? DistanceKm { get; set; }
        public int? DurationMinutes { get; set; }
    }

    public class StationInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class BusInfoDto
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string? Type { get; set; }
    }

    public class SeatInfoDto
    {
        public int Id { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
