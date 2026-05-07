using OtobusBiletRezervasyon.DTOs.Search;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class SeferIndexViewModel
    {
        public SearchQueryDto AramaFormu { get; set; } = new();
        public IEnumerable<StationInfoDto> Istasyonlar { get; set; } = Enumerable.Empty<StationInfoDto>();
        public IEnumerable<DepartureResponseDto> YaklasanSeferler { get; set; } = Enumerable.Empty<DepartureResponseDto>();
        /// <summary>
        /// Maximum price among all upcoming departures (independent of pagination).
        /// Used for scaling the price slider on the frontend.
        /// </summary>
        public decimal MaxPrice { get; set; } = 100m;
    }

    public class SeferAramaContextViewModel
    {
        public SearchQueryDto AramaKriterleri { get; set; } = new();
        public IEnumerable<StationInfoDto> Istasyonlar { get; set; } = Enumerable.Empty<StationInfoDto>();
        public IEnumerable<DepartureResponseDto> Sonuclar { get; set; } = Enumerable.Empty<DepartureResponseDto>();
        public StationInfoDto? KalkisIstasyonu { get; set; }
        public StationInfoDto? VarisIstasyonu { get; set; }
        /// <summary>
        /// Maximum price among all upcoming departures (independent of pagination).
        /// </summary>
        public decimal MaxPrice { get; set; } = 100m;
    }
}
