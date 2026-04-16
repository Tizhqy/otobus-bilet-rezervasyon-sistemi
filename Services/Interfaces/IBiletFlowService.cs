using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Services.FlowModels;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IBiletFlowService
    {
        Task<IEnumerable<TicketResponseDto>> GetUserTicketsAsync(int userId);
        Task<ServiceResult<TicketResponseDto>> GetTicketDetayForUserAsync(int ticketId, int userId, bool isAdmin);
        Task<ServiceResult<BiletSatinAlViewModel>> HazirlaSatinAlSayfasiAsync(int seferId, int koltukId);
        Task<ServiceResult<BiletSatinAlViewModel>> HazirlaSatinAlSayfasiAsync(CreateTicketDto formDto);
        Task<ServiceResult<TicketResponseDto>> SatinAlAsync(int userId, CreateTicketDto createTicketDto);
        Task<ServiceResult<TicketResponseDto>> SatinAlFormAsync(
            int userId,
            int seferId,
            int koltukId,
            string yolcuAd,
            string yolcuSoyad,
            string? yolcuTc,
            string odemeYontemi);
        Task<ServiceResult> IptalAsync(int ticketId, int userId);
        Task<bool> KoltukMusaitMiAsync(int seferId, int koltukId);
        Task<bool> KoltuklarMusaitMiAsync(int seferId, IEnumerable<int> koltukIds);
    }
}
