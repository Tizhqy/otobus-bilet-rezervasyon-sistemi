using System.ComponentModel.DataAnnotations;

namespace OtobusBiletRezervasyon.DTOs.Admin
{
    /// <summary>
    /// Admin Otobus ekleme/duzenleme icin DTO.
    /// Mass assignment koruması saglar — sadece izin verilen alanlar bind edilir.
    /// </summary>
    public class AdminBusDto
    {
        [Required(ErrorMessage = "Plaka numarasi zorunludur.")]
        [MaxLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kapasite zorunludur.")]
        [Range(1, 100, ErrorMessage = "Kapasite 1-100 arasi olmalidir.")]
        public int Capacity { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Admin Rota ekleme/duzenleme icin DTO.
    /// </summary>
    public class AdminRouteDto
    {
        [Required(ErrorMessage = "Kalkis istasyonu zorunludur.")]
        public int OriginStationId { get; set; }

        [Required(ErrorMessage = "Varis istasyonu zorunludur.")]
        public int DestinationStationId { get; set; }

        [Range(0, 10000)]
        public int? DistanceKm { get; set; }

        [Range(0, 10000)]
        public int? DurationMinutes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Admin Istasyon ekleme/duzenleme icin DTO.
    /// </summary>
    public class AdminStationDto
    {
        [Required(ErrorMessage = "Istasyon adi zorunludur.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sehir zorunludur.")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Admin Sefer ekleme/duzenleme icin DTO.
    /// </summary>
    public class AdminDepartureDto
    {
        [Required(ErrorMessage = "Rota zorunludur.")]
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Otobus zorunludur.")]
        public int BusId { get; set; }

        [Required(ErrorMessage = "Kalkis zamani zorunludur.")]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "Varis zamani zorunludur.")]
        public DateTime ArrivalTime { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur.")]
        [Range(0.01, 100000, ErrorMessage = "Fiyat 0'dan buyuk olmalidir.")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
