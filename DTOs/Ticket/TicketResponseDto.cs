namespace OtobusBiletRezervasyon.DTOs.Ticket
{
    public class TicketResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DepartureInfoDto Departure { get; set; } = null!;
        public List<PassengerInfoDto> Passengers { get; set; } = new List<PassengerInfoDto>();
        public PaymentResponseDto? Payment { get; set; }
    }

    public class DepartureInfoDto
    {
        public int Id { get; set; }
        public string OriginStation { get; set; } = string.Empty;
        public string OriginCity { get; set; } = string.Empty;
        public string DestinationStation { get; set; } = string.Empty;
        public string DestinationCity { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string BusPlateNumber { get; set; } = string.Empty;
        public string? BusType { get; set; }
    }

    public class PassengerInfoDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
    }

    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
