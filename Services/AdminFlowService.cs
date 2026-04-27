using OtobusBiletRezervasyon.DTOs.Admin;
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
            var allBuses = (await _adminService.GetAllBusesAsync()).ToList();
            var allRoutes = (await _adminService.GetAllRoutesAsync()).ToList();

            return new AdminDashboardViewModel
            {
                Stats = await _adminService.GetDashboardStatsAsync(),
                RecentLogs = (await _logService.GetRecentLogsAsync(10)).ToList(),
                Buses = allBuses.Take(10).ToList(),
                Routes = allRoutes.Take(10).ToList(),
                Users = userPage.Users.ToList(),
                UpcomingDepartures = (await _adminService.GetUpcomingDeparturesAsync(100)).ToList(),
                RouteOptions = allRoutes
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.OriginStation?.City ?? r.OriginStation?.Name ?? string.Empty)
                    .ThenBy(r => r.DestinationStation?.City ?? r.DestinationStation?.Name ?? string.Empty)
                    .ToList(),
                BusOptions = allBuses
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.PlateNumber)
                    .ToList()
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

        public async Task<ServiceResult> OtobusEkleAsync(AdminBusDto dto, int adminId, string ipAddress)
        {
            try
            {
                var bus = new Bus
                {
                    PlateNumber = (dto.PlateNumber ?? string.Empty).ToUpperInvariant(),
                    Capacity = dto.Capacity,
                    Type = dto.Type,
                    IsActive = true
                };

                await _adminService.CreateBusAsync(bus);
                await _logService.LogAdminActionAsync(adminId, "OTOBUS_EKLE", $"Otobus eklendi: {bus.PlateNumber}", ipAddress);
                return ServiceResult.Ok("Otobus eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> OtobusDuzenleAsync(int id, AdminBusDto dto, int adminId, string ipAddress)
        {
            var existing = await _adminService.GetBusByIdAsync(id);
            if (existing == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Otobus bulunamadi.");

            try
            {
                existing.PlateNumber = (dto.PlateNumber ?? string.Empty).ToUpperInvariant();
                existing.Capacity = dto.Capacity;
                existing.Type = dto.Type;
                existing.IsActive = dto.IsActive;

                await _adminService.UpdateBusAsync(existing);
                await _logService.LogAdminActionAsync(adminId, "OTOBUS_DUZENLE", $"Otobus guncellendi: {existing.PlateNumber}", ipAddress);
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

        public async Task<ServiceResult> RotaEkleAsync(AdminRouteDto dto, int adminId, string ipAddress)
        {
            if (dto.OriginStationId == dto.DestinationStationId)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Kalkis ve varis istasyonlari ayni olamaz.");
            }

            try
            {
                var route = new Route
                {
                    OriginStationId = dto.OriginStationId,
                    DestinationStationId = dto.DestinationStationId,
                    DistanceKm = dto.DistanceKm,
                    DurationMinutes = dto.DurationMinutes,
                    IsActive = true
                };

                await _adminService.CreateRouteAsync(route);
                await _logService.LogAdminActionAsync(adminId, "ROTA_EKLE", $"Rota #{route.Id} eklendi", ipAddress);
                return ServiceResult.Ok("Rota eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> RotaDuzenleAsync(int id, AdminRouteDto dto, int adminId, string ipAddress)
        {
            if (dto.OriginStationId == dto.DestinationStationId)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Kalkis ve varis istasyonlari ayni olamaz.");
            }

            var existing = await _adminService.GetRouteByIdAsync(id);
            if (existing == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Rota bulunamadi.");

            try
            {
                existing.OriginStationId = dto.OriginStationId;
                existing.DestinationStationId = dto.DestinationStationId;
                existing.DistanceKm = dto.DistanceKm;
                existing.DurationMinutes = dto.DurationMinutes;
                existing.IsActive = dto.IsActive;

                await _adminService.UpdateRouteAsync(existing);
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

        public async Task<ServiceResult> IstasyonEkleAsync(AdminStationDto dto, int adminId, string ipAddress)
        {
            try
            {
                var station = new Station
                {
                    Name = dto.Name,
                    City = dto.City,
                    Address = dto.Address,
                    IsActive = true
                };

                await _adminService.CreateStationAsync(station);
                await _logService.LogAdminActionAsync(adminId, "ISTASYON_EKLE", $"Istasyon eklendi: {station.Name}, {station.City}", ipAddress);
                return ServiceResult.Ok("Istasyon eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> IstasyonDuzenleAsync(int id, AdminStationDto dto, int adminId, string ipAddress)
        {
            var existing = await _adminService.GetStationByIdAsync(id);
            if (existing == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Istasyon bulunamadi.");

            try
            {
                existing.Name = dto.Name;
                existing.City = dto.City;
                existing.Address = dto.Address;
                existing.IsActive = dto.IsActive;

                await _adminService.UpdateStationAsync(existing);
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

        public async Task<ServiceResult> SeferEkleAsync(AdminDepartureDto dto, int adminId, string ipAddress)
        {
            var departure = new Departure
            {
                RouteId = dto.RouteId,
                BusId = dto.BusId,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                Price = dto.Price,
                IsActive = true
            };

            var validation = ValidateDeparture(departure);
            if (!validation.Success)
                return validation;

            try
            {
                var created = await _adminService.CreateDepartureAsync(departure);
                await _logService.LogAdminActionAsync(adminId, "SEFER_EKLE", $"Sefer #{created.Id} eklendi", ipAddress);
                return ServiceResult.Ok("Sefer eklendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> SeferDuzenleAsync(int id, AdminDepartureDto dto, int adminId, string ipAddress)
        {
            var existing = await _adminService.GetDepartureByIdAsync(id);
            if (existing == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Sefer bulunamadi.");

            existing.RouteId = dto.RouteId;
            existing.BusId = dto.BusId;
            existing.DepartureTime = dto.DepartureTime;
            existing.ArrivalTime = dto.ArrivalTime;
            existing.Price = dto.Price;
            existing.IsActive = dto.IsActive;

            var validation = ValidateDeparture(existing);
            if (!validation.Success)
                return validation;

            try
            {
                await _adminService.UpdateDepartureAsync(existing);
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

        public async Task<ServiceResult> SeferFiyatGuncelleAsync(
            AdminSingleDeparturePriceUpdateDto request,
            int adminId,
            string ipAddress)
        {
            if (request.DepartureId <= 0)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Gecersiz sefer secimi.");

            if (!AdminPriceInputParser.TryParseDecimalFlexible(request.NewPrice, out var parsedPrice) || parsedPrice <= 0m)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Fiyat 0'dan buyuk olmalidir.");

            if (parsedPrice > 999999.99m)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Fiyat 999999.99'dan buyuk olamaz.");

            try
            {
                var updated = await _adminService.UpdateDeparturePriceAsync(request.DepartureId, parsedPrice);
                await _logService.LogAdminActionAsync(
                    adminId,
                    "SEFER_FIYAT_DUZENLE",
                    $"Sefer #{updated.Id} fiyati {updated.Price:0.00} olarak guncellendi.",
                    ipAddress);
                return ServiceResult.Ok("Sefer fiyati guncellendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
        }

        public async Task<ServiceResult> SeferFiyatTopluGuncelleAsync(
            AdminBulkDeparturePriceUpdateDto request,
            int adminId,
            string ipAddress)
        {
            if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Value.Date)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Bitis tarihi, baslangic tarihinden once olamaz.");
            }

            var useMultiplier = request.Mode == AdminBulkPriceUpdateMode.Multiply;
            var rawValue = useMultiplier ? request.Multiplier : request.FixedPrice;
            if (!AdminPriceInputParser.TryParseDecimalFlexible(rawValue, out var parsedValue) || parsedValue <= 0m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    useMultiplier
                        ? "Toplu guncelleme icin gecerli bir carpan giriniz."
                        : "Toplu guncelleme icin gecerli bir sabit fiyat giriniz.");
            }

            if (useMultiplier && parsedValue > 10m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Toplu carpan 10'dan buyuk olamaz.");
            }

            if (!useMultiplier && parsedValue > 999999.99m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Sabit fiyat 999999.99'dan buyuk olamaz.");
            }

            try
            {
                var affected = await _adminService.BulkUpdateDeparturePricesAsync(
                    request.RouteId,
                    request.BusId,
                    request.StartDate,
                    request.EndDate,
                    useMultiplier,
                    parsedValue);

                if (affected == 0)
                {
                    return ServiceResult.Fail(
                        ServiceResultType.NotFound,
                        "Secilen kriterlere uygun guncellenecek aktif/yaklasan sefer bulunamadi.");
                }

                var criteriaText =
                    $"Route={request.RouteId?.ToString() ?? "all"}, " +
                    $"Bus={request.BusId?.ToString() ?? "all"}, " +
                    $"Date={request.StartDate?.ToString("yyyy-MM-dd") ?? "-"}..{request.EndDate?.ToString("yyyy-MM-dd") ?? "-"}, " +
                    $"Mode={(useMultiplier ? "multiply" : "fixed")}, Value={parsedValue:0.00}";

                await _logService.LogAdminActionAsync(
                    adminId,
                    "SEFER_FIYAT_TOPLU",
                    $"{affected} seferin fiyati guncellendi ({criteriaText}).",
                    ipAddress);

                return ServiceResult.Ok($"{affected} seferin fiyati guncellendi.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.ValidationError, ex.Message);
            }
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
            // Negatif veya minimum altindaki degerleri otomatik duzelt
            gunSayisi = Math.Max(gunSayisi, AppConfig.MinLogRetentionDays);

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

            if (departure.Price <= 0m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Sefer fiyati 0'dan buyuk olmalidir.");
            }

            return ServiceResult.Ok();
        }
    }
}
