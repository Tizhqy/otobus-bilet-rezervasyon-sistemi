using System.ComponentModel.DataAnnotations;

namespace OtobusBiletRezervasyon.DTOs.Ticket
{
    public class CreateTicketDto
    {
        [Required(ErrorMessage = "Departure ID is required")]
        public int DepartureId { get; set; }

        [Required(ErrorMessage = "At least one passenger is required")]
        [MinLength(1, ErrorMessage = "At least one passenger is required")]
        public List<PassengerDto> Passengers { get; set; } = new List<PassengerDto>();

        [Required(ErrorMessage = "Payment information is required")]
        public PaymentInfoDto Payment { get; set; } = null!;
    }

    public class PassengerDto
    {
        [Required(ErrorMessage = "Seat ID is required")]
        public int SeatId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? IdNumber { get; set; }
    }

    public class PaymentInfoDto
    {
        [Required(ErrorMessage = "Payment method is required")]
        public string Method { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Transaction ID is required")]
        public string TransactionId { get; set; } = string.Empty;
    }
}
