namespace QueueBookingAPI.DTOs
{
    public class QueueStatusDto
    {
        public int CurrentServing { get; set; }
        public int? WindowNumber { get; set; }
        public int WaitingCount { get; set; }
        public int CompletedCount { get; set; }
        public int TotalBookings { get; set; }
        public string Date { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? LastRecallTime { get; set; }
    }
}

