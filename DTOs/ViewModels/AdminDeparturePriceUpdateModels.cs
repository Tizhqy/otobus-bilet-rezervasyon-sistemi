using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace OtobusBiletRezervasyon.DTOs.ViewModels
{
    public enum AdminBulkPriceUpdateMode
    {
        Multiply = 1,
        SetFixed = 2
    }

    public class AdminSingleDeparturePriceUpdateDto
    {
        [Required]
        public int DepartureId { get; set; }

        [Required(ErrorMessage = "Yeni fiyat zorunludur.")]
        public string NewPrice { get; set; } = string.Empty;
    }

    public class AdminBulkDeparturePriceUpdateDto : IValidatableObject
    {
        public int? RouteId { get; set; }
        public int? BusId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public AdminBulkPriceUpdateMode Mode { get; set; } = AdminBulkPriceUpdateMode.Multiply;

        public string? Multiplier { get; set; } = "1";

        public string? FixedPrice { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
            {
                yield return new ValidationResult(
                    "Bitis tarihi, baslangic tarihinden once olamaz.",
                    new[] { nameof(StartDate), nameof(EndDate) });
            }

            var multiplierParsed = AdminPriceInputParser.TryParseDecimalFlexible(Multiplier, out var parsedMultiplier);
            if (Mode == AdminBulkPriceUpdateMode.Multiply)
            {
                if (!multiplierParsed || parsedMultiplier <= 0m)
                {
                    yield return new ValidationResult(
                        "Toplu carpan guncellemesi icin gecerli bir carpan giriniz.",
                        new[] { nameof(Multiplier) });
                }
                else if (parsedMultiplier > 10m)
                {
                    yield return new ValidationResult(
                        "Toplu carpan 10'dan buyuk olamaz.",
                        new[] { nameof(Multiplier) });
                }
            }

            var fixedPriceParsed = AdminPriceInputParser.TryParseDecimalFlexible(FixedPrice, out var parsedFixedPrice);
            if (Mode == AdminBulkPriceUpdateMode.SetFixed)
            {
                if (!fixedPriceParsed || parsedFixedPrice <= 0m)
                {
                    yield return new ValidationResult(
                        "Toplu sabit fiyat guncellemesi icin gecerli bir fiyat giriniz.",
                        new[] { nameof(FixedPrice) });
                }
                else if (parsedFixedPrice > 999999.99m)
                {
                    yield return new ValidationResult(
                        "Sabit fiyat 999999.99'dan buyuk olamaz.",
                        new[] { nameof(FixedPrice) });
                }
            }
        }
    }

    public static class AdminPriceInputParser
    {
        public static bool TryParseDecimalFlexible(string? rawValue, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            var input = rawValue.Trim();

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                return true;

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                return true;

            var compact = input.Replace(" ", string.Empty);
            var lastComma = compact.LastIndexOf(',');
            var lastDot = compact.LastIndexOf('.');

            if (lastComma >= 0 && lastDot >= 0)
            {
                var decimalSeparatorIndex = Math.Max(lastComma, lastDot);
                var integerPart = compact[..decimalSeparatorIndex].Replace(",", string.Empty).Replace(".", string.Empty);
                var fractionPart = compact[(decimalSeparatorIndex + 1)..].Replace(",", string.Empty).Replace(".", string.Empty);
                compact = $"{integerPart}.{fractionPart}";
            }
            else if (lastComma >= 0)
            {
                compact = compact.Replace(".", string.Empty).Replace(',', '.');
            }
            else
            {
                compact = compact.Replace(",", string.Empty);
            }

            return decimal.TryParse(compact, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }
}
