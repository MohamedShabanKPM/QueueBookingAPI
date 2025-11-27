namespace QueueBookingAPI.DTOs
{
    public class BookingDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string BookingDateSelection { get; set; } = "today";
    }

    public class BookingResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime BookingDate { get; set; }
        public int QueueNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? WindowNumber { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class BookingUpdateDto
    {
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }
}

