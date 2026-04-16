using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class AdminUserPageViewModel
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public int ToplamKayit { get; set; }
        public int MevcutSayfa { get; set; }
        public int ToplamSayfa { get; set; }
        public string? Ara { get; set; }
    }
}
