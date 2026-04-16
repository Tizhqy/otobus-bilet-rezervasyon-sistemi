using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class AdminLogPageViewModel
    {
        public IEnumerable<Log> Logs { get; set; } = Enumerable.Empty<Log>();
        public int ToplamKayit { get; set; }
        public int MevcutSayfa { get; set; }
        public int ToplamSayfa { get; set; }
        public string? IslemFiltre { get; set; }
        public int? KullaniciFiltre { get; set; }
    }
}
