using OtobusBiletRezervasyon.DTOs.Admin;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class AdminFlowService : IAdminFlowService
    {
        private readonly IAdminService _adminService;
        private readonly ILogService _logService;
        private readonly ICouponRepository _couponRepository;

        public AdminFlowService(IAdminService adminService, ILogService logService, ICouponRepository couponRepository)
        {
            _adminService = adminService;
            _logService = logService;
            _couponRepository = couponRepository;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync(string? depSearch = null, int depPage = 1)
        {
            if (depPage < 1) depPage = 1;

            var userPage = await _adminService.GetUsersPageAsync(null, 1, 10);
            var allBuses = (await _adminService.GetAllBusesAsync()).ToList();
            var allRoutes = (await _adminService.GetAllRoutesAsync()).ToList();

            int pageSize = AppConfig.AdminDeparturePageSize;
            var (departures, totalCount) = await _adminService.GetUpcomingDeparturesPageAsync(depSearch, depPage, pageSize);
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (depPage > totalPages) depPage = totalPages;

            return new AdminDashboardViewModel
            {
                Stats = await _adminService.GetDashboardStatsAsync(),
                RecentLogs = (await _logService.GetRecentLogsAsync(10)).ToList(),
                Buses = allBuses.Take(10).ToList(),
                Routes = allRoutes.Take(10).ToList(),
                Users = userPage.Users.ToList(),
                UpcomingDepartures = departures.ToList(),
                RouteOptions = allRoutes
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.OriginStation?.City ?? r.OriginStation?.Name ?? string.Empty)
                    .ThenBy(r => r.DestinationStation?.City ?? r.DestinationStation?.Name ?? string.Empty)
                    .ToList(),
                BusOptions = allBuses
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.PlateNumber)
                    .ToList(),
                DeparturesTotalCount = totalCount,
                DeparturesCurrentPage = depPage,
                DeparturesTotalPages = totalPages,
                DepartureSearchTerm = depSearch
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
                await _logService.LogAdminActionAsync(adminId, "OTOBUS_EKLE", $"Bus added: {bus.PlateNumber}", ipAddress);
                return ServiceResult.Ok("Bus added.");
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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Bus not found.");

            try
            {
                existing.PlateNumber = (dto.PlateNumber ?? string.Empty).ToUpperInvariant();
                existing.Capacity = dto.Capacity;
                existing.Type = dto.Type;
                existing.IsActive = dto.IsActive;

                await _adminService.UpdateBusAsync(existing);
                await _logService.LogAdminActionAsync(adminId, "OTOBUS_DUZENLE", $"Bus updated: {existing.PlateNumber}", ipAddress);
                return ServiceResult.Ok("Bus updated.");
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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Bus not found.");

            var toggled = await _adminService.ToggleBusStatusAsync(id);
            if (!toggled)
                return ServiceResult.Fail(ServiceResultType.Error, "Failed to change bus status.");

            await _logService.LogAdminActionAsync(adminId, "OTOBUS_DURUM", $"Bus status changed: {bus.PlateNumber}", ipAddress);
            return ServiceResult.Ok("Bus status changed.");
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
                    "Origin and destination stations cannot be the same.");
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
                await _logService.LogAdminActionAsync(adminId, "ROTA_EKLE", $"Route #{route.Id} added", ipAddress);
                return ServiceResult.Ok("Route added.");
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
                    "Origin and destination stations cannot be the same.");
            }

            var existing = await _adminService.GetRouteByIdAsync(id);
            if (existing == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Route not found.");

            try
            {
                existing.OriginStationId = dto.OriginStationId;
                existing.DestinationStationId = dto.DestinationStationId;
                existing.DistanceKm = dto.DistanceKm;
                existing.DurationMinutes = dto.DurationMinutes;
                existing.IsActive = dto.IsActive;

                await _adminService.UpdateRouteAsync(existing);
                await _logService.LogAdminActionAsync(adminId, "ROTA_DUZENLE", $"Route #{id} updated", ipAddress);
                return ServiceResult.Ok("Route updated.");
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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Route not found.");

            await _logService.LogAdminActionAsync(adminId, "ROTA_DURUM", $"Route #{id} status changed", ipAddress);
            return ServiceResult.Ok("Route status changed.");
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
                await _logService.LogAdminActionAsync(adminId, "ISTASYON_EKLE", $"Station added: {station.Name}, {station.City}", ipAddress);
                return ServiceResult.Ok("Station added.");
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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Station not found.");

            try
            {
                existing.Name = dto.Name;
                existing.City = dto.City;
                existing.Address = dto.Address;
                existing.IsActive = dto.IsActive;

                await _adminService.UpdateStationAsync(existing);
                await _logService.LogAdminActionAsync(adminId, "ISTASYON_DUZENLE", $"Station #{id} updated", ipAddress);
                return ServiceResult.Ok("Station updated.");
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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Station not found.");

            await _logService.LogAdminActionAsync(adminId, "ISTASYON_DURUM", $"Station #{id} status changed", ipAddress);
            return ServiceResult.Ok("Station status changed.");
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
                await _logService.LogAdminActionAsync(adminId, "SEFER_EKLE", $"Departure #{created.Id} added", ipAddress);
                return ServiceResult.Ok("Departure added.");
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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Departure not found.");

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
                await _logService.LogAdminActionAsync(adminId, "SEFER_DUZENLE", $"Departure #{id} updated", ipAddress);
                return ServiceResult.Ok("Departure updated.");
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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Departure not found.");

            await _logService.LogAdminActionAsync(adminId, "SEFER_DURUM", $"Departure #{id} status changed", ipAddress);
            return ServiceResult.Ok("Departure status changed.");
        }

        public async Task<ServiceResult> SeferFiyatGuncelleAsync(
            AdminSingleDeparturePriceUpdateDto request,
            int adminId,
            string ipAddress)
        {
            if (request.DepartureId <= 0)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Invalid departure selection.");

            if (!AdminPriceInputParser.TryParseDecimalFlexible(request.NewPrice, out var parsedPrice) || parsedPrice <= 0m)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Price must be greater than 0.");

            if (parsedPrice > 999999.99m)
                return ServiceResult.Fail(ServiceResultType.ValidationError, "Price cannot be greater than 999999.99.");

            try
            {
                var updated = await _adminService.UpdateDeparturePriceAsync(request.DepartureId, parsedPrice);
                await _logService.LogAdminActionAsync(
                    adminId,
                    "SEFER_FIYAT_DUZENLE",
                    $"Departure #{updated.Id} price updated to {updated.Price:0.00}.",
                    ipAddress);
                return ServiceResult.Ok("Departure price updated.");
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
                    "End date cannot be before start date.");
            }

            var useMultiplier = request.Mode == AdminBulkPriceUpdateMode.Multiply;
            var rawValue = useMultiplier ? request.Multiplier : request.FixedPrice;
            if (!AdminPriceInputParser.TryParseDecimalFlexible(rawValue, out var parsedValue) || parsedValue <= 0m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    useMultiplier
                        ? "Please enter a valid multiplier for bulk update."
                        : "Please enter a valid fixed price for bulk update.");
            }

            if (useMultiplier && parsedValue > 10m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Bulk multiplier cannot be greater than 10.");
            }

            if (!useMultiplier && parsedValue > 999999.99m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Fixed price cannot be greater than 999999.99.");
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
                        "No active/upcoming departures found matching the selected criteria.");
                }

                var criteriaText =
                    $"Route={request.RouteId?.ToString() ?? "all"}, " +
                    $"Bus={request.BusId?.ToString() ?? "all"}, " +
                    $"Date={request.StartDate?.ToString("yyyy-MM-dd") ?? "-"}..{request.EndDate?.ToString("yyyy-MM-dd") ?? "-"}, " +
                    $"Mode={(useMultiplier ? "multiply" : "fixed")}, Value={parsedValue:0.00}";

                await _logService.LogAdminActionAsync(
                    adminId,
                    "SEFER_FIYAT_TOPLU",
                    $"Price updated for {affected} departures ({criteriaText}).",
                    ipAddress);

                return ServiceResult.Ok($"Price updated for {affected} departures.");
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
                return ServiceResult.Fail(ServiceResultType.Forbidden, "You cannot change your own role.");

            var user = await _adminService.GetUserByIdAsync(kullaniciId);
            if (user == null)
                return ServiceResult.Fail(ServiceResultType.NotFound, "User not found.");

            user.RoleId = roleId;
            await _adminService.UpdateUserAsync(user);
            await _logService.LogAdminActionAsync(adminId, "ROL_DEGISTIR", $"User #{kullaniciId} role set to #{roleId}", ipAddress);
            return ServiceResult.Ok("Role updated.");
        }

        public async Task<ServiceResult> KullaniciDurumDegistirAsync(int adminId, int hedefKullaniciId, string ipAddress)
        {
            if (hedefKullaniciId == adminId)
                return ServiceResult.Fail(ServiceResultType.Forbidden, "You cannot deactivate your own account.");

            var toggled = await _adminService.ToggleUserStatusAsync(hedefKullaniciId);
            if (!toggled)
                return ServiceResult.Fail(ServiceResultType.NotFound, "User not found.");

            await _logService.LogAdminActionAsync(adminId, "KULLANICI_DURUM", $"User #{hedefKullaniciId} status changed", ipAddress);
            return ServiceResult.Ok("User status changed.");
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
                return ServiceResult.Fail(ServiceResultType.Error, "Log cleanup process failed.");

            await _logService.LogAdminActionAsync(adminId, "LOG_TEMIZLE", $"Logs older than {gunSayisi} days deleted", ipAddress);
            return ServiceResult.Ok($"Logs older than {gunSayisi} days cleaned.");
        }

        private static ServiceResult ValidateDeparture(Departure departure)
        {
            if (departure.DepartureTime <= DateTime.UtcNow)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Departure time cannot be in the past.");
            }

            if (departure.ArrivalTime <= departure.DepartureTime)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Arrival time must be after departure time.");
            }

            if (departure.Price <= 0m)
            {
                return ServiceResult.Fail(
                    ServiceResultType.ValidationError,
                    "Departure price must be greater than 0.");
            }

            return ServiceResult.Ok();
        }

        public async Task<IEnumerable<Coupon>> GetKuponlarAsync()
        {
            return await _couponRepository.GetAllAsync();
        }

        public async Task<ServiceResult> KuponEkleAsync(AdminCouponDto dto, int adminUserId, string ipAddress)
        {
            try
            {
                var existing = await _couponRepository.GetByCodeAsync(dto.Code);
                if (existing != null)
                {
                    return ServiceResult.Fail(ServiceResultType.Conflict, "This coupon code already exists.");
                }

                var coupon = new Coupon
                {
                    Code = dto.Code.ToUpperInvariant(),
                    DiscountAmount = dto.DiscountAmount,
                    DiscountType = dto.DiscountType,
                    ValidUntil = dto.ValidUntil,
                    IsActive = dto.IsActive
                };

                await _couponRepository.CreateAsync(coupon);
                await _logService.LogAdminActionAsync(adminUserId, "KUPON_EKLE", $"Coupon added: {coupon.Code}", ipAddress);
                return ServiceResult.Ok("Coupon added.");
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail(ServiceResultType.Error, "An error occurred while adding coupon: " + ex.Message);
            }
        }

        public async Task<ServiceResult> KuponDurumDegistirAsync(int id, int adminUserId, string ipAddress)
        {
            var coupon = await _couponRepository.GetByIdAsync(id);
            if (coupon == null)
            {
                return ServiceResult.Fail(ServiceResultType.NotFound, "Coupon not found.");
            }

            coupon.IsActive = !coupon.IsActive;
            await _couponRepository.UpdateAsync(coupon);
            await _logService.LogAdminActionAsync(adminUserId, "KUPON_DURUM", $"Coupon {coupon.Code} status set to {coupon.IsActive}", ipAddress);
            return ServiceResult.Ok("Coupon status updated.");
        }

        public async Task<ServiceResult> KuponSilAsync(int id, int adminUserId, string ipAddress)
        {
            var coupon = await _couponRepository.GetByIdAsync(id);
            if (coupon == null)
            {
                return ServiceResult.Fail(ServiceResultType.NotFound, "Coupon not found.");
            }

            // We normally shouldn't randomly delete coupons if they have been used, but CouponUsage table has cascade delete or we just do a soft-delete physically.
            // Wait, does ICouponRepository.DeleteAsync actually delete? Yes. Let's try.
            try
            {
                var deleted = await _couponRepository.DeleteAsync(id);
                if (!deleted)
                    return ServiceResult.Fail(ServiceResultType.Error, "Coupon could not be deleted.");

                await _logService.LogAdminActionAsync(adminUserId, "KUPON_SIL", $"Coupon deleted: {coupon.Code}", ipAddress);
                return ServiceResult.Ok("Coupon deleted.");
            }
            catch (Exception)
            {
                return ServiceResult.Fail(ServiceResultType.Conflict, "This coupon cannot be deleted because it is currently in use or has been used in the past. Try deactivating it instead.");
            }
        }
    }
}
