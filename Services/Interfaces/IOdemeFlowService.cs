using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Services.FlowModels;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IOdemeFlowService
    {
        Task<ServiceResult<OdemeSayfasiViewModel>> HazirlaOdemeSayfasiAsync(int biletId, int userId);
        Task<ServiceResult<OdemeTamamlamaViewModel>> OdemeyiTamamlaAsync(
            int biletId,
            int userId,
            string odemeYontemi,
            string paymentToken,
            string? cardLast4);
        Task<(bool authorized, bool expired, int seconds)> KalanSureAsync(int biletId, int userId);
    }
}
