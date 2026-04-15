using OtobusBiletRezervasyon.DTOs.Search;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class SeferDetayViewModel
    {
        public DepartureResponseDto Sefer { get; set; } = null!;
        public List<SeatInfoDto> Seats { get; set; } = new();
    }
}

