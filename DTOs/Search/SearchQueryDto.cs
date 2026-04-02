using System.ComponentModel.DataAnnotations;

namespace OtobusBiletRezervasyon.DTOs.Search
{
    public class SearchQueryDto
    {
        [Required(ErrorMessage = "Origin station ID is required")]
        public int OriginStationId { get; set; }

        [Required(ErrorMessage = "Destination station ID is required")]
        public int DestinationStationId { get; set; }

        [Required(ErrorMessage = "Travel date is required")]
        public DateTime TravelDate { get; set; }

        public int PassengerCount { get; set; } = 1;
    }
}
