using OtobusBiletRezervasyon.DTOs.Search;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class SeferIndexViewModel
    {
        public SearchQueryDto AramaFormu { get; set; } = new();
        public IEnumerable<StationInfoDto> Istasyonlar { get; set; } = Enumerable.Empty<StationInfoDto>();
        public IEnumerable<DepartureResponseDto> YaklasanSeferler { get; set; } = Enumerable.Empty<DepartureResponseDto>();
    }

    public class SeferAramaContextViewModel
    {
        public SearchQueryDto AramaKriterleri { get; set; } = new();
        public IEnumerable<StationInfoDto> Istasyonlar { get; set; } = Enumerable.Empty<StationInfoDto>();
        public IEnumerable<DepartureResponseDto> Sonuclar { get; set; } = Enumerable.Empty<DepartureResponseDto>();
        public StationInfoDto? KalkisIstasyonu { get; set; }
        public StationInfoDto? VarisIstasyonu { get; set; }
    }
}
