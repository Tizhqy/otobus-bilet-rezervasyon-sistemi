using OtobusBiletRezervasyon.DTOs.Ticket;
using System.ComponentModel.DataAnnotations;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class OdemeSayfasiViewModel
    {
        public TicketResponseDto Ticket { get; set; } = null!;
        public int KalanSaniye { get; set; }
    }

    public class OdemeTamamlamaViewModel
    {
        public string ReferenceNo { get; set; } = string.Empty;
        public string OdemeYontemi { get; set; } = string.Empty;
    }

    public class OdemeTamamlamaIstekDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Gecersiz bilet.")]
        public int BiletId { get; set; }

        [Required]
        [MaxLength(32)]
        public string OdemeYontemi { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[A-Fa-f0-9]{64}$", ErrorMessage = "Odeme token'i gecersiz.")]
        public string PaymentToken { get; set; } = string.Empty;

        [RegularExpression(@"^\d{4}$", ErrorMessage = "Kart son 4 hane formati gecersiz.")]
        public string? CardLast4 { get; set; }
    }
}
