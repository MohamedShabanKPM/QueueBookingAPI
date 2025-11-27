using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueueBookingAPI.Models
{
    public class QueueWindow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Number { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public User? CurrentUser { get; set; }
    }
}

