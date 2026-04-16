using OtobusBiletRezervasyon.DTOs.Search;
using OtobusBiletRezervasyon.DTOs.Ticket;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class BiletSatinAlViewModel
    {
        public CreateTicketDto Form { get; set; } = new();
        public DepartureResponseDto? Sefer { get; set; }
        public SeatInfoDto? SecilenKoltuk { get; set; }
        public int SeferId { get; set; }
        public int KoltukId { get; set; }
    }
}
