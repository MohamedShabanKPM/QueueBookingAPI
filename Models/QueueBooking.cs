using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueueBookingAPI.Models
{
    public class QueueBooking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Email { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string BookingDateSelection { get; set; } = "today";

        [Required]
        public int QueueNumber { get; set; }

        public int? WindowId { get; set; }
        [ForeignKey("WindowId")]
        public QueueWindow? Window { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "waiting";

        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }

        public int? StartedByUserId { get; set; }
        [ForeignKey("StartedByUserId")]
        public User? StartedByUser { get; set; }

        [StringLength(20)]
        public string? TimeTaken { get; set; }

        public string? Notes { get; set; }

        [NotMapped]
        public string DisplayName => $"#{QueueNumber} - {Name}" + (Window != null ? $" - Window {Window.Number}" : "");
    }
}

