using Microsoft.EntityFrameworkCore;
using QueueBookingAPI.Models;

namespace QueueBookingAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<QueueBooking> QueueBookings { get; set; }
        public DbSet<QueueWindow> QueueWindows { get; set; }
        public DbSet<QueueTracking> QueueTrackings { get; set; }
        public DbSet<QueueUserWindow> QueueUserWindows { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // QueueWindow unique constraint
            modelBuilder.Entity<QueueWindow>()
                .HasIndex(w => w.Number)
                .IsUnique();

            // QueueBooking indexes
            modelBuilder.Entity<QueueBooking>()
                .HasIndex(b => new { b.Phone, b.BookingDate })
                .HasDatabaseName("IX_QueueBooking_Phone_BookingDate");

            modelBuilder.Entity<QueueBooking>()
                .HasIndex(b => b.QueueNumber)
                .HasDatabaseName("IX_QueueBooking_QueueNumber");

            modelBuilder.Entity<QueueBooking>()
                .HasIndex(b => b.Status)
                .HasDatabaseName("IX_QueueBooking_Status");

            // QueueTracking unique date
            modelBuilder.Entity<QueueTracking>()
                .HasIndex(t => t.Date)
                .IsUnique();

            // QueueUserWindow indexes
            modelBuilder.Entity<QueueUserWindow>()
                .HasIndex(u => new { u.UserId, u.IsActive })
                .HasDatabaseName("IX_QueueUserWindow_UserId_IsActive");

            modelBuilder.Entity<QueueUserWindow>()
                .HasIndex(u => new { u.WindowId, u.IsActive })
                .HasDatabaseName("IX_QueueUserWindow_WindowId_IsActive");
        }
    }
}

