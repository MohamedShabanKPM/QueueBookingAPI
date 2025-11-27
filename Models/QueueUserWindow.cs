using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueueBookingAPI.Models
{
    public class QueueUserWindow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public int WindowId { get; set; }
        [ForeignKey("WindowId")]
        public QueueWindow Window { get; set; } = null!;

        [Required]
        public DateTime AssignTime { get; set; } = DateTime.Now;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? ReleaseTime { get; set; }
    }
}

