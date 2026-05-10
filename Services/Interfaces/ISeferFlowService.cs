using OtobusBiletRezervasyon.DTOs.Search;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Services.FlowModels;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface ISeferFlowService
    {
        Task<SeferIndexViewModel> GetIndexDataAsync();
        Task<SeferAramaContextViewModel> GetBosAramaSayfasiAsync();
        Task<ServiceResult<SeferAramaContextViewModel>> AraAsync(SearchQueryDto searchQuery);
        Task<ServiceResult<SeferDetayViewModel>> GetDetayAsync(int seferId);
        Task<ServiceResult<IEnumerable<SeatInfoDto>>> GetKoltukDurumuAsync(int seferId);
        Task<ServiceResult<IEnumerable<SeatInfoDto>>> GetMusaitKoltuklarAsync(int seferId);
        Task<IEnumerable<StationInfoDto>> IstasyonAraAsync(string query);
        Task<IEnumerable<StationInfoDto>> TumIstasyonlarAsync();
        Task<IEnumerable<DepartureResponseDto>> GetYaklasanSeferlerAsync(int count = 10);
    }
}
