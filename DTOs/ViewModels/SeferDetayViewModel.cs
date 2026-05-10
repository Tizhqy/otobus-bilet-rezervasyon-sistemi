using OtobusBiletRezervasyon.DTOs.Search;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public class SeferDetayViewModel
    {
        public DepartureResponseDto Sefer { get; set; } = null!;
        public List<SeatInfoDto> Seats { get; set; } = new();

        // ── Bus Layout Properties ────────────────────────────────────
        /// <summary>
        /// Koridorun sol tarafindaki koltuk sayisi (ornegin 2+1'de sol=1, 2+2'de sol=2)
        /// </summary>
        public int LeftSeatsPerRow { get; set; } = 2;

        /// <summary>
        /// Koridorun sag tarafindaki koltuk sayisi
        /// </summary>
        public int RightSeatsPerRow { get; set; } = 2;

        /// <summary>
        /// Gosterim etiketi: "2+1", "2+2", "1+1"
        /// </summary>
        public string LayoutLabel { get; set; } = "2+2";

        /// <summary>
        /// CSS class'i: "layout-2-1", "layout-2-2", "layout-1-1"
        /// </summary>
        public string LayoutClass { get; set; } = "layout-2-2";

        /// <summary>
        /// Satirlara bolunmus koltuklar
        /// </summary>
        public List<SeatRowViewModel> SeatRows { get; set; } = new();

        // ── Computed ─────────────────────────────────────────────────
        public int SeatsPerRow => LeftSeatsPerRow + RightSeatsPerRow;

        /// <summary>
        /// Bus type string'inden layout hesaplar ve SeatRows olusturur.
        /// </summary>
        public static SeferDetayViewModel Create(
            DepartureResponseDto sefer,
            List<SeatInfoDto> seats)
        {
            var vm = new SeferDetayViewModel
            {
                Sefer = sefer,
                Seats = seats
            };

            vm.ParseBusLayout(sefer.Bus.Type);
            vm.BuildSeatRows();

            return vm;
        }

        private void ParseBusLayout(string? busType)
        {
            var token = (busType ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", string.Empty);

            if (token.Contains("2+1") || token.Contains("1+2"))
            {
                LeftSeatsPerRow = 1;
                RightSeatsPerRow = 2;
                LayoutLabel = "2+1";
                LayoutClass = "layout-2-1";
            }
            else if (token.Contains("1+1"))
            {
                LeftSeatsPerRow = 1;
                RightSeatsPerRow = 1;
                LayoutLabel = "1+1";
                LayoutClass = "layout-1-1";
            }
            else
            {
                LeftSeatsPerRow = 2;
                RightSeatsPerRow = 2;
                LayoutLabel = "2+2";
                LayoutClass = "layout-2-2";
            }
        }

        private void BuildSeatRows()
        {
            SeatRows = new List<SeatRowViewModel>();
            var seatsPerRow = Math.Max(1, LeftSeatsPerRow + RightSeatsPerRow);

            for (var i = 0; i < Seats.Count; i += seatsPerRow)
            {
                var rowSeats = Seats.Skip(i).Take(seatsPerRow).ToList();
                SeatRows.Add(new SeatRowViewModel
                {
                    LeftSeats = rowSeats.Take(LeftSeatsPerRow).ToList(),
                    RightSeats = rowSeats.Skip(LeftSeatsPerRow).Take(RightSeatsPerRow).ToList()
                });
            }
        }
    }

    public class SeatRowViewModel
    {
        public List<SeatInfoDto> LeftSeats { get; set; } = new();
        public List<SeatInfoDto> RightSeats { get; set; } = new();
    }
}
