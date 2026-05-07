using OtobusBiletRezervasyon.DTOs.Ticket;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    /// <summary>
    /// Bilet PDF'i ve QR kodu üretim servisi.
    /// Single Responsibility: Sadece PDF/QR üretimi ile ilgilenir.
    /// </summary>
    public interface IPdfTicketService
    {
        /// <summary>
        /// Verilen bilet bilgilerine göre PDF dökümanı oluşturur.
        /// </summary>
        byte[] GenerateTicketPdf(TicketResponseDto ticket);

        /// <summary>
        /// Verilen bilet bilgilerine göre QR kod görselini PNG olarak üretir.
        /// </summary>
        byte[] GenerateQrCode(TicketResponseDto ticket);

        /// <summary>
        /// QR kodunu base64 data URI olarak döndürür (img src="data:image/png;base64,..." için).
        /// </summary>
        string GenerateQrCodeBase64DataUri(TicketResponseDto ticket);
    }
}
