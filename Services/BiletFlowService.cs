using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace OtobusBiletRezervasyon.Services
{
    public class BiletFlowService : IBiletFlowService
    {
        private readonly ITicketService _ticketService;
        private readonly ISearchService _searchService;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BiletFlowService(
            ITicketService ticketService,
            ISearchService searchService,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor)
        {
            _ticketService = ticketService;
            _searchService = searchService;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<IEnumerable<TicketResponseDto>> GetUserTicketsAsync(int userId)
        {
            return _ticketService.GetUserTicketsAsync(userId);
        }

        public async Task<ServiceResult<TicketResponseDto>> GetTicketDetayForUserAsync(int ticketId, int userId, bool isAdmin)
        {
            if (ticketId <= 0)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.NotFound, "Bilet bulunamadi.");

            var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
            if (ticket == null)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.NotFound, "Bilet bulunamadi.");

            if (ticket.UserId != userId && !isAdmin)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Forbidden, "Bu bilete erisim yetkiniz yok.");

            return ServiceResult<TicketResponseDto>.Ok(ticket);
        }

        public async Task<ServiceResult<BiletSatinAlViewModel>> HazirlaSatinAlSayfasiAsync(int seferId, int[] koltukIds)
        {
            if (seferId <= 0 || koltukIds == null || !koltukIds.Any())
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.ValidationError, "Gecersiz sefer veya koltuk bilgisi.");

            if (koltukIds.Length > AppConfig.MaxPassengerPerTicket)
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.ValidationError, $"En fazla {AppConfig.MaxPassengerPerTicket} koltuk secebilirsiniz.");

            var areAvailable = await _ticketService.AreSeatAvailableAsync(seferId, koltukIds);
            if (!areAvailable)
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.Conflict, "Sectiginiz koltuklardan biri veya birkaci dolu. Lutfen baska koltuk secin.");

            var departure = await _searchService.GetDepartureByIdAsync(seferId);
            if (departure == null)
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.NotFound, "Sefer bulunamadi.");

            if (AppConfig.IsTicketSalesClosed(departure.DepartureTime))
            {
                return ServiceResult<BiletSatinAlViewModel>.Fail(
                    ServiceResultType.Expired,
                    $"Bu sefer icin bilet satisi kalkisa {AppConfig.TicketSalesCutoffMinutesBeforeDeparture} dakikadan az kala kapatilir.");
            }

            var seats = await _searchService.GetSeatsForDepartureAsync(seferId);
            var selectedSeats = seats.Where(s => koltukIds.Contains(s.Id)).ToList();
            
            var passengers = selectedSeats.Select(s => new PassengerDto { SeatId = s.Id }).ToList();

            var actualPrice = departure.AvailableSeats <= 10 && departure.AvailableSeats > 0 ? departure.Price * 1.1m : departure.Price;

            var model = new BiletSatinAlViewModel
            {
                Sefer = departure,
                SecilenKoltuklar = selectedSeats,
                SeferId = seferId,
                KoltukIds = selectedSeats.Select(x => x.Id).ToList(),
                Form = new CreateTicketDto
                {
                    DepartureId = seferId,
                    Passengers = passengers,
                    Payment = new PaymentInfoDto
                    {
                        Method = "CreditCard",
                        Amount = actualPrice * passengers.Count,
                        TransactionId = Guid.NewGuid().ToString("N")
                    }
                }
            };

            return ServiceResult<BiletSatinAlViewModel>.Ok(model);
        }

        public async Task<ServiceResult<BiletSatinAlViewModel>> HazirlaSatinAlSayfasiAsync(CreateTicketDto formDto)
        {
            formDto.Passengers ??= new List<PassengerDto>();
            formDto.Payment ??= new PaymentInfoDto();

            if (!formDto.Passengers.Any())
                formDto.Passengers.Add(new PassengerDto());

            var selectedSeatIds = formDto.Passengers.Select(p => p.SeatId).ToArray();
            var departure = await _searchService.GetDepartureByIdAsync(formDto.DepartureId);

            if (departure == null)
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.NotFound, "Sefer bulunamadi.");

            var seats = await _searchService.GetSeatsForDepartureAsync(formDto.DepartureId);
            var selectedSeats = seats.Where(s => selectedSeatIds.Contains(s.Id)).ToList();

            return ServiceResult<BiletSatinAlViewModel>.Ok(new BiletSatinAlViewModel
            {
                Sefer = departure,
                SecilenKoltuklar = selectedSeats,
                SeferId = formDto.DepartureId,
                KoltukIds = selectedSeats.Select(s => s.Id).ToList(),
                Form = formDto
            });
        }

        public async Task<ServiceResult<TicketResponseDto>> SatinAlAsync(int userId, CreateTicketDto createTicketDto)
        {
            createTicketDto.Passengers ??= new List<PassengerDto>();
            createTicketDto.Payment ??= new PaymentInfoDto();

            if (!createTicketDto.Passengers.Any())
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.ValidationError, "En az bir yolcu bilgisi gerekli.");

            var departure = await _searchService.GetDepartureByIdAsync(createTicketDto.DepartureId);
            if (departure == null)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.NotFound, "Sefer bulunamadi.");

            if (AppConfig.IsTicketSalesClosed(departure.DepartureTime))
            {
                return ServiceResult<TicketResponseDto>.Fail(
                    ServiceResultType.Expired,
                    $"Bu sefer icin bilet satisi kalkisa {AppConfig.TicketSalesCutoffMinutesBeforeDeparture} dakikadan az kala kapatilir.");
            }

            if (!TryNormalizePaymentMethod(createTicketDto.Payment.Method, out var normalizedMethod))
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.ValidationError, "Gecersiz odeme yontemi.");

            createTicketDto.Payment.Method = normalizedMethod;
            createTicketDto.Payment.Amount = departure.Price * createTicketDto.Passengers.Count;
            if (string.IsNullOrWhiteSpace(createTicketDto.Payment.TransactionId))
                createTicketDto.Payment.TransactionId = Guid.NewGuid().ToString("N");

            try
            {
                var ticket = await _ticketService.PurchaseTicketAsync(userId, createTicketDto);
                await _logService.LogTicketPurchaseAsync(userId, ticket.Id, GetClientIpAddress());
                return ServiceResult<TicketResponseDto>.Ok(ticket);
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Conflict, ex.Message);
            }
            catch
            {
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Error, "Bilet satin alinirken bir hata olustu. Lutfen tekrar deneyin.");
            }
        }

        public async Task<ServiceResult<TicketResponseDto>> SatinAlFormAsync(
            int userId,
            int seferId,
            int koltukId,
            string yolcuAd,
            string yolcuSoyad,
            string? yolcuTc,
            string odemeYontemi)
        {
            if (string.IsNullOrWhiteSpace(yolcuAd) || string.IsNullOrWhiteSpace(yolcuSoyad))
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.ValidationError, "Yolcu adi ve soyadi zorunludur.");

            var isAvailable = await _ticketService.IsSeatAvailableAsync(seferId, koltukId);
            if (!isAvailable)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Conflict, "Bu koltuk artik musait degil.");

            var dto = new CreateTicketDto
            {
                DepartureId = seferId,
                Passengers = new List<PassengerDto>
                {
                    new()
                    {
                        SeatId = koltukId,
                        FirstName = yolcuAd,
                        LastName = yolcuSoyad,
                        IdNumber = yolcuTc
                    }
                },
                Payment = new PaymentInfoDto
                {
                    Method = odemeYontemi,
                    TransactionId = Guid.NewGuid().ToString("N")
                }
            };

            return await SatinAlAsync(userId, dto);
        }

        public async Task<ServiceResult> IptalAsync(int ticketId, int userId)
        {
            if (ticketId <= 0)
                return ServiceResult.Fail(ServiceResultType.NotFound, "Bilet bulunamadi.");

            try
            {
                var success = await _ticketService.CancelTicketAsync(ticketId, userId);
                if (!success)
                {
                    return ServiceResult.Fail(
                        ServiceResultType.Conflict,
                        $"Bilet iptal edilemedi. Bilet bulunamadi, zaten iptal edilmis veya kalkisa {AppConfig.MinCancellationMinutesBeforeDeparture} dakikadan az kalmis olabilir.");
                }

                await _logService.LogTicketCancellationAsync(userId, ticketId, GetClientIpAddress());
                return ServiceResult.Ok("Biletiniz iptal edildi.");
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult.Fail(ServiceResultType.Forbidden, "Bu bilet icin iptal yetkiniz yok.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.Conflict, ex.Message);
            }
            catch
            {
                return ServiceResult.Fail(ServiceResultType.Error, "Bilet iptal edilirken bir hata olustu.");
            }
        }

        public Task<bool> KoltukMusaitMiAsync(int seferId, int koltukId)
        {
            return _ticketService.IsSeatAvailableAsync(seferId, koltukId);
        }

        public Task<bool> KoltuklarMusaitMiAsync(int seferId, IEnumerable<int> koltukIds)
        {
            return _ticketService.AreSeatAvailableAsync(seferId, koltukIds);
        }



        private static bool TryNormalizePaymentMethod(string? method, out string normalizedMethod)
        {
            normalizedMethod = string.Empty;
            if (string.IsNullOrWhiteSpace(method))
                return false;

            var compact = method
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();

            normalizedMethod = compact switch
            {
                "creditcard" => "CreditCard",
                "debitcard" => "DebitCard",
                "paypal" => "Paypal",
                _ => string.Empty
            };

            return !string.IsNullOrEmpty(normalizedMethod);
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
