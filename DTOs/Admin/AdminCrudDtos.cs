using System.ComponentModel.DataAnnotations;

namespace OtobusBiletRezervasyon.DTOs.Admin
{
    /// <summary>
    /// DTO for Admin Bus add/edit.
    /// Provides mass assignment protection — only allowed fields are bound.
    /// </summary>
    public class AdminBusDto
    {
        [Required(ErrorMessage = "Plate number is required.")]
        [MaxLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100.")]
        public int Capacity { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for Admin Route add/edit.
    /// </summary>
    public class AdminRouteDto
    {
        [Required(ErrorMessage = "Origin station is required.")]
        public int OriginStationId { get; set; }

        [Required(ErrorMessage = "Destination station is required.")]
        public int DestinationStationId { get; set; }

        [Range(0, 10000)]
        public int? DistanceKm { get; set; }

        [Range(0, 10000)]
        public int? DurationMinutes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for Admin Station add/edit.
    /// </summary>
    public class AdminStationDto
    {
        [Required(ErrorMessage = "Station name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for Admin Departure add/edit.
    /// </summary>
    public class AdminDepartureDto
    {
        [Required(ErrorMessage = "Route is required.")]
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Bus is required.")]
        public int BusId { get; set; }

        [Required(ErrorMessage = "Departure time is required.")]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "Arrival time is required.")]
        public DateTime ArrivalTime { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 100000, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
