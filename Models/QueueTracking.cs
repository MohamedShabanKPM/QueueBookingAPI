using System.ComponentModel.DataAnnotations;

namespace QueueBookingAPI.Models
{
    public class QueueTracking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public int CurrentServingNumber { get; set; } = 0;

        [Required]
        public int TotalBookings { get; set; } = 0;

        [Required]
        public int WaitingCount { get; set; } = 0;

        [Required]
        public int CompletedCount { get; set; } = 0;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? LastRecallTime { get; set; }
    }
}

