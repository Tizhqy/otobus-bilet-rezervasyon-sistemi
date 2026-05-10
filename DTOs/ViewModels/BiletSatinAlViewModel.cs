using OtobusBiletRezervasyon.DTOs.Search;
using OtobusBiletRezervasyon.DTOs.Ticket;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class BiletSatinAlViewModel
    {
        public CreateTicketDto Form { get; set; } = new();
        public DepartureResponseDto? Sefer { get; set; }
        public List<SeatInfoDto> SecilenKoltuklar { get; set; } = new();
        public int SeferId { get; set; }
        public List<int> KoltukIds { get; set; } = new();
    }
}
