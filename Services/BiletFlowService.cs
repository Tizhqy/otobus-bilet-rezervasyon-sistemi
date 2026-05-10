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
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.NotFound, "Ticket not found.");

            var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
            if (ticket == null)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.NotFound, "Ticket not found.");

            if (ticket.UserId != userId && !isAdmin)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Forbidden, "You do not have permission to access this ticket.");

            if (ticket.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || 
                ticket.Status.Equals("BEKLEMEDE", StringComparison.OrdinalIgnoreCase))
            {
                if (ticket.CreatedAt.AddMinutes(AppConfig.PaymentTimeoutMinutes) < DateTime.UtcNow)
                {
                    await _ticketService.CancelTicketAsync(ticketId, ticket.UserId);
                    await _logService.LogTicketCancellationAsync(userId, ticketId, GetClientIpAddress());
                    
                    ticket = await _ticketService.GetTicketByIdAsync(ticketId);
                }
            }

            return ServiceResult<TicketResponseDto>.Ok(ticket!);
        }

        public async Task<ServiceResult<TicketResponseDto>> GetTicketForDownloadAsync(int ticketId, int userId, bool isAdmin)
        {
            var result = await GetTicketDetayForUserAsync(ticketId, userId, isAdmin);
            if (!result.Success)
                return result;

            var ticket = result.Data!;
            if (!ticket.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) && 
                !ticket.Status.Equals("ONAYLANDI", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Conflict, "Sadece ödemesi tamamlanmış biletlerin çıktısı alınabilir.");
            }

            return ServiceResult<TicketResponseDto>.Ok(ticket);
        }

        public async Task<ServiceResult<BiletSatinAlViewModel>> HazirlaSatinAlSayfasiAsync(int seferId, int[] koltukIds)
        {
            if (seferId <= 0 || koltukIds == null || !koltukIds.Any())
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.ValidationError, "Invalid departure or seat information.");

            if (koltukIds.Length > AppConfig.MaxPassengerPerTicket)
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.ValidationError, $"You can select up to {AppConfig.MaxPassengerPerTicket} seats.");

            var areAvailable = await _ticketService.AreSeatAvailableAsync(seferId, koltukIds);
            if (!areAvailable)
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.Conflict, "One or more selected seats are taken. Please choose another seat.");

            var departure = await _searchService.GetDepartureByIdAsync(seferId);
            if (departure == null)
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.NotFound, "Departure not found.");

            if (AppConfig.IsTicketSalesClosed(departure.DepartureTime))
            {
                return ServiceResult<BiletSatinAlViewModel>.Fail(
                    ServiceResultType.Expired,
                    $"Ticket sales for this departure are closed within {AppConfig.TicketSalesCutoffMinutesBeforeDeparture} minutes of departure.");
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
                return ServiceResult<BiletSatinAlViewModel>.Fail(ServiceResultType.NotFound, "Departure not found.");

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
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.ValidationError, "At least one passenger information is required.");

            var departure = await _searchService.GetDepartureByIdAsync(createTicketDto.DepartureId);
            if (departure == null)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.NotFound, "Departure not found.");

            if (AppConfig.IsTicketSalesClosed(departure.DepartureTime))
            {
                return ServiceResult<TicketResponseDto>.Fail(
                    ServiceResultType.Expired,
                    $"Ticket sales for this departure are closed within {AppConfig.TicketSalesCutoffMinutesBeforeDeparture} minutes of departure.");
            }

            if (!TryNormalizePaymentMethod(createTicketDto.Payment.Method, out var normalizedMethod))
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.ValidationError, "Invalid payment method.");

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
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Error, "An error occurred while purchasing the ticket. Please try again.");
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
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.ValidationError, "Passenger first and last name are required.");

            var isAvailable = await _ticketService.IsSeatAvailableAsync(seferId, koltukId);
            if (!isAvailable)
                return ServiceResult<TicketResponseDto>.Fail(ServiceResultType.Conflict, "This seat is no longer available.");

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
                return ServiceResult.Fail(ServiceResultType.NotFound, "Ticket not found.");

            try
            {
                var success = await _ticketService.CancelTicketAsync(ticketId, userId);
                if (!success)
                {
                    return ServiceResult.Fail(
                        ServiceResultType.Conflict,
                        $"Ticket could not be cancelled. It might not be found, already cancelled, or departure is within {AppConfig.MinCancellationMinutesBeforeDeparture} minutes.");
                }

                await _logService.LogTicketCancellationAsync(userId, ticketId, GetClientIpAddress());
                return ServiceResult.Ok("Your ticket has been cancelled.");
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult.Fail(ServiceResultType.Forbidden, "You do not have permission to cancel this ticket.");
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Fail(ServiceResultType.Conflict, ex.Message);
            }
            catch
            {
                return ServiceResult.Fail(ServiceResultType.Error, "An error occurred while cancelling the ticket.");
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
