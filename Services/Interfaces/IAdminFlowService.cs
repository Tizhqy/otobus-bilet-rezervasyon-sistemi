using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Services.FlowModels;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IAdminFlowService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync();

        Task<IEnumerable<Bus>> GetOtobuslerAsync();
        Task<Bus?> GetOtobusByIdAsync(int id);
        Task<ServiceResult> OtobusEkleAsync(Bus bus, int adminId, string ipAddress);
        Task<ServiceResult> OtobusDuzenleAsync(int id, Bus bus, int adminId, string ipAddress);
        Task<ServiceResult> OtobusDurumDegistirAsync(int id, int adminId, string ipAddress);

        Task<IEnumerable<Route>> GetRotalarAsync();
        Task<IEnumerable<Station>> GetIstasyonSecenekleriAsync();
        Task<Route?> GetRotaByIdAsync(int id);
        Task<ServiceResult> RotaEkleAsync(Route route, int adminId, string ipAddress);
        Task<ServiceResult> RotaDuzenleAsync(int id, Route route, int adminId, string ipAddress);
        Task<ServiceResult> RotaDurumDegistirAsync(int id, int adminId, string ipAddress);

        Task<IEnumerable<Station>> GetIstasyonlarAsync();
        Task<Station?> GetIstasyonByIdAsync(int id);
        Task<ServiceResult> IstasyonEkleAsync(Station station, int adminId, string ipAddress);
        Task<ServiceResult> IstasyonDuzenleAsync(int id, Station station, int adminId, string ipAddress);
        Task<ServiceResult> IstasyonDurumDegistirAsync(int id, int adminId, string ipAddress);

        Task<IEnumerable<Departure>> GetSeferlerAsync();
        Task<(IEnumerable<Route> Rotalar, IEnumerable<Bus> Otobusler)> GetSeferFormDataAsync();
        Task<Departure?> GetSeferByIdAsync(int id);
        Task<ServiceResult> SeferEkleAsync(Departure departure, int adminId, string ipAddress);
        Task<ServiceResult> SeferDuzenleAsync(int id, Departure departure, int adminId, string ipAddress);
        Task<ServiceResult> SeferDurumDegistirAsync(int id, int adminId, string ipAddress);

        Task<AdminUserPageViewModel> GetKullanicilarAsync(string? ara, int sayfa);
        Task<ServiceResult> KullaniciRolDegistirAsync(int adminId, int kullaniciId, int roleId, string ipAddress);
        Task<ServiceResult> KullaniciDurumDegistirAsync(int adminId, int hedefKullaniciId, string ipAddress);

        Task<AdminLogPageViewModel> GetLoglarAsync(string? islem, int? kullaniciId, int sayfa);
        Task<ServiceResult> LoglarTemizleAsync(int gunSayisi, int adminId, string ipAddress);
    }
}
