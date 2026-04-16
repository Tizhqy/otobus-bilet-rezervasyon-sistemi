using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class AdminFlowService : IAdminFlowService
    {
        private readonly IAdminService _adminService;
        private readonly ILogService _logService;

        public AdminFlowService(IAdminService adminService, ILogService logService)
        {
            _adminService = adminService;
            _logService = logService;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var userPage = await _adminService.GetUsersPageAsync(null, 1, 10);

            return new AdminDashboardViewModel
            {
                Stats = await _adminService.GetDashboardStatsAsync(),
                RecentLogs = (await _logService.GetRecentLogsAsync(10)).ToList(),
                Buses = (await _adminService.GetAllBusesAsync()).Take(10).ToList(),
                Routes = (await _adminService.GetAllRoutesAsync()).Take(10).ToList(),
                Users = userPage.Users.ToList()
            };
        }

        public Task<IEnumerable<Bus>> GetOtobuslerAsync()
        {
            return _adminService.GetAllBusesAsync();
        }

        public Task<Bus?> GetOtobusByIdAsync(int id)
        {
            return _adminService.GetBusByIdAsync(id);
        }

        public async Task<ServiceResult> OtobusEkleAsync(Bus bus, int adminId, string ipAddress)
        {
            try
            {
                bus.PlateNumber = (bus.PlateNumber ?? string.Empty).ToUpperInvariant();
                bus.IsActive = true;

                await _adminService.CreateBusAsync(bus);
                await _logService.LogAdminActionAsync(adminId, "OTOBUS_EKLE", $"Otobus eklendi: {bus.PlateNumber}", ipAddress);
                return ServiceResult.Ok("Otobus eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> OtobusDuzenleAsync(int id, Bus bus, int adminId, string ipAddress)
        {
            if (id != bus.Id)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Gecersiz otobus bilgisi.");

            try
            {
                bus.PlateNumber = (bus.PlateNumber ?? string.Empty).ToUpperInvariant();
                await _adminService.UpdateBusAsync(bus);
                await _logService.LogAdminActionAsync(adminId, "OTOBUS_DUZENLE", $"Otobus guncellendi: {bus.PlateNumber}", ipAddress);
                return ServiceResult.Ok("Otobus guncellendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> OtobusDurumDegistirAsync(int id, int adminId, string ipAddress)
        {
            var bus = await _adminService.GetBusByIdAsync(id);
            if (bus == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Otobus bulunamadi.");

            var toggled = await _adminService.ToggleBusStatusAsync(id);
            if (!toggled)
                return ServiceResult.Fail(ServiceResultType.Error, "Otobus durumu degistirilemedi.");

            await _logService.LogAdminActionAsync(adminId, "OTOBUS_DURUM", $"Otobus durumu degistirildi: {bus.PlateNumber}", ipAddress);
            return ServiceResult.Ok("Otobus durumu degistirildi.");
        }

        public Task<IEnumerable<Route>> GetRotalarAsync()
        {
            return _adminService.GetAllRoutesAsync();
        }

        public Task<IEnumerable<Station>> GetIstasyonSecenekleriAsync()
        {
            return _adminService.GetAllStationsAsync();
        }

        public Task<Route?> GetRotaByIdAsync(int id)
        {
            return _adminService.GetRouteByIdAsync(id);
        }

        public async Task<ServiceResult> RotaEkleAsync(Route route, int adminId, string ipAddress)
        {
            if (route.OriginStationId == route.DestinationStationId)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Kalkis ve varis istasyonlari ayni olamaz.");
            }

            try
            {
                route.IsActive = true;
                await _adminService.CreateRouteAsync(route);
                await _logService.LogAdminActionAsync(adminId, "ROTA_EKLE", $"Rota #{route.Id} eklendi", ipAddress);
                return ServiceResult.Ok("Rota eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> RotaDuzenleAsync(int id, Route route, int adminId, string ipAddress)
        {
            if (id != route.Id)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Gecersiz rota bilgisi.");

            if (route.OriginStationId == route.DestinationStationId)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Kalkis ve varis istasyonlari ayni olamaz.");
            }

            try
            {
                await _adminService.UpdateRouteAsync(route);
                await _logService.LogAdminActionAsync(adminId, "ROTA_DUZENLE", $"Rota #{id} guncellendi", ipAddress);
                return ServiceResult.Ok("Rota guncellendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> RotaDurumDegistirAsync(int id, int adminId, string ipAddress)
        {
            var toggled = await _adminService.ToggleRouteStatusAsync(id);
            if (!toggled)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Rota bulunamadi.");

            await _logService.LogAdminActionAsync(adminId, "ROTA_DURUM", $"Rota #{id} durumu degistirildi", ipAddress);
            return ServiceResult.Ok("Rota durumu degistirildi.");
        }

        public Task<IEnumerable<Station>> GetIstasyonlarAsync()
        {
            return _adminService.GetAllStationsAsync();
        }

        public Task<Station?> GetIstasyonByIdAsync(int id)
        {
            return _adminService.GetStationByIdAsync(id);
        }

        public async Task<ServiceResult> IstasyonEkleAsync(Station station, int adminId, string ipAddress)
        {
            try
            {
                station.IsActive = true;
                await _adminService.CreateStationAsync(station);
                await _logService.LogAdminActionAsync(adminId, "ISTASYON_EKLE", $"Istasyon eklendi: {station.Name}, {station.City}", ipAddress);
                return ServiceResult.Ok("Istasyon eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> IstasyonDuzenleAsync(int id, Station station, int adminId, string ipAddress)
        {
            if (id != station.Id)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Gecersiz istasyon bilgisi.");

            try
            {
                await _adminService.UpdateStationAsync(station);
                await _logService.LogAdminActionAsync(adminId, "ISTASYON_DUZENLE", $"Istasyon #{id} guncellendi", ipAddress);
                return ServiceResult.Ok("Istasyon guncellendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> IstasyonDurumDegistirAsync(int id, int adminId, string ipAddress)
        {
            var toggled = await _adminService.ToggleStationStatusAsync(id);
            if (!toggled)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Istasyon bulunamadi.");

            await _logService.LogAdminActionAsync(adminId, "ISTASYON_DURUM", $"Istasyon #{id} durumu degistirildi", ipAddress);
            return ServiceResult.Ok("Istasyon durumu degistirildi.");
        }

        public Task<IEnumerable<Departure>> GetSeferlerAsync()
        {
            return _adminService.GetAllDeparturesAsync();
        }

        public async Task<(IEnumerable<Route> Rotalar, IEnumerable<Bus> Otobusler)> GetSeferFormDataAsync()
        {
            var rotalar = await _adminService.GetAllRoutesAsync();
            var otobusler = await _adminService.GetAllBusesAsync();
            return (rotalar, otobusler);
        }

        public Task<Departure?> GetSeferByIdAsync(int id)
        {
            return _adminService.GetDepartureByIdAsync(id);
        }

        public async Task<ServiceResult> SeferEkleAsync(Departure departure, int adminId, string ipAddress)
        {
            var validation = ValidateDeparture(departure);
            if (!validation.Success)
                return validation;

            try
            {
                departure.IsActive = true;
                var created = await _adminService.CreateDepartureAsync(departure);
                await _logService.LogAdminActionAsync(adminId, "SEFER_EKLE", $"Sefer #{created.Id} eklendi", ipAddress);
                return ServiceResult.Ok("Sefer eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> SeferDuzenleAsync(int id, Departure departure, int adminId, string ipAddress)
        {
            if (id != departure.Id)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Gecersiz sefer bilgisi.");

            var validation = ValidateDeparture(departure);
            if (!validation.Success)
                return validation;

            try
            {
                await _adminService.UpdateDepartureAsync(departure);
                await _logService.LogAdminActionAsync(adminId, "SEFER_DUZENLE", $"Sefer #{id} guncellendi", ipAddress);
                return ServiceResult.Ok("Sefer guncellendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> SeferDurumDegistirAsync(int id, int adminId, string ipAddress)
        {
            var toggled = await _adminService.ToggleDepartureStatusAsync(id);
            if (!toggled)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Sefer bulunamadi.");

            await _logService.LogAdminActionAsync(adminId, "SEFER_DURUM", $"Sefer #{id} durumu degistirildi", ipAddress);
            return ServiceResult.Ok("Sefer durumu degistirildi.");
        }

        public async Task<AdminUserPageViewModel> GetKullanicilarAsync(string? ara, int sayfa)
        {
            if (sayfa <= 0) sayfa = 1;

            var normalizedSearch = string.IsNullOrWhiteSpace(ara) ? null : ara.Trim();
            int sayfaBoyutu = AppConfig.AdminUserPageSize;
            var pageResult = await _adminService.GetUsersPageAsync(normalizedSearch, sayfa, sayfaBoyutu);
            int toplamKayit = pageResult.TotalCount;
            int toplamSayfa = Math.Max(1, (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu));
            if (sayfa > toplamSayfa)
            {
                sayfa = toplamSayfa;
                pageResult = await _adminService.GetUsersPageAsync(normalizedSearch, sayfa, sayfaBoyutu);
            }

            return new AdminUserPageViewModel
            {
                Users = pageResult.Users.ToList(),
                ToplamKayit = toplamKayit,
                MevcutSayfa = sayfa,
                ToplamSayfa = toplamSayfa,
                Ara = normalizedSearch
            };
        }

        public async Task<ServiceResult> KullaniciRolDegistirAsync(int adminId, int kullaniciId, int roleId, string ipAddress)
        {
            if (kullaniciId == adminId)
                return ServiceResult.Fail(ServiceResultType.Forbidden, "Kendi rolunuzu degistiremezsiniz.");

            var user = await _adminService.GetUserByIdAsync(kullaniciId);
            if (user == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Kullanici bulunamadi.");

            user.RoleId = roleId;
            await _adminService.UpdateUserAsync(user);
            await _logService.LogAdminActionAsync(adminId, "ROL_DEGISTIR", $"Kullanici #{kullaniciId} rolu #{roleId} yapildi", ipAddress);
            return ServiceResult.Ok("Rol guncellendi.");
        }

        public async Task<ServiceResult> KullaniciDurumDegistirAsync(int adminId, int hedefKullaniciId, string ipAddress)
        {
            if (hedefKullaniciId == adminId)
                return ServiceResult.Fail(ServiceResultType.Forbidden, "Kendi hesabinizi pasife alamazsiniz.");

            var toggled = await _adminService.ToggleUserStatusAsync(hedefKullaniciId);
            if (!toggled)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Kullanici bulunamadi.");

            await _logService.LogAdminActionAsync(adminId, "KULLANICI_DURUM", $"Kullanici #{hedefKullaniciId} durumu degistirildi", ipAddress);
            return ServiceResult.Ok("Kullanici durumu degistirildi.");
        }

        public async Task<AdminLogPageViewModel> GetLoglarAsync(string? islem, int? kullaniciId, int sayfa)
        {
            if (sayfa <= 0) sayfa = 1;

            IEnumerable<Log> logs;
            if (!string.IsNullOrWhiteSpace(islem))
            {
                logs = await _logService.GetLogsByActionAsync(islem);
            }
            else if (kullaniciId.HasValue)
            {
                logs = await _logService.GetLogsByUserIdAsync(kullaniciId.Value);
            }
            else
            {
                logs = await _logService.GetLogsAsync();
            }

            var logList = logs.ToList();
            int toplamKayit = logList.Count;
            int sayfaBoyutu = AppConfig.LogPageSize;
            int toplamSayfa = Math.Max(1, (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu));
            sayfa = Math.Min(sayfa, toplamSayfa);

            var pagedLogs = logList
                .OrderByDescending(l => l.CreatedAt)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .ToList();

            return new AdminLogPageViewModel
            {
                Logs = pagedLogs,
                ToplamKayit = toplamKayit,
                MevcutSayfa = sayfa,
                ToplamSayfa = toplamSayfa,
                IslemFiltre = islem,
                KullaniciFiltre = kullaniciId
            };
        }

        public async Task<ServiceResult> LoglarTemizleAsync(int gunSayisi, int adminId, string ipAddress)
        {
            if (gunSayisi < AppConfig.MinLogRetentionDays)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    $"En az {AppConfig.MinLogRetentionDays} gunluk loglar saklanmalidir.");
            }

            var deleted = await _logService.DeleteOldLogsAsync(gunSayisi);
            if (!deleted)
                return ServiceResult.Fail(ServiceResultType.Error, "Log temizleme islemi basarisiz.");

            await _logService.LogAdminActionAsync(adminId, "LOG_TEMIZLE", $"{gunSayisi} gunden eski loglar silindi", ipAddress);
            return ServiceResult.Ok($"{gunSayisi} gunden eski loglar temizlendi.");
        }

        private static ServiceResult ValidateDeparture(Departure departure)
        {
            if (departure.DepartureTime <= DateTime.UtcNow)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Kalkis tarihi gecmis olamaz.");
            }

            if (departure.ArrivalTime <= departure.DepartureTime)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Varis tarihi kalkis tarihinden sonra olmalidir.");
            }

            return ServiceResult.Ok();
        }
    }
}
