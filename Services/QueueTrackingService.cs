using Microsoft.EntityFrameworkCore;
using QueueBookingAPI.Data;
using QueueBookingAPI.Models;
using QueueBookingAPI.DTOs;

namespace QueueBookingAPI.Services
{
    public class QueueTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QueueTrackingService> _logger;

        public QueueTrackingService(ApplicationDbContext context, ILogger<QueueTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<QueueStatusDto> GetQueueStatusAsync()
        {
            var tracking = await GetTodayTrackingAsync();
            await RefreshStatisticsAsync(tracking);

            var windowNumber = (int?)null;
            if (tracking.CurrentServingNumber > 0)
            {
                var booking = await _context.QueueBookings
                    .Include(b => b.Window)
                    .Where(b => b.QueueNumber == tracking.CurrentServingNumber &&
                               b.BookingDate >= tracking.Date &&
                               b.BookingDate < tracking.Date.AddDays(1))
                    .OrderByDescending(b => b.Id)
                    .FirstOrDefaultAsync();

                if (booking?.Window != null)
                {
                    windowNumber = booking.Window.Number;
                }
            }

            return new QueueStatusDto
            {
                CurrentServing = tracking.CurrentServingNumber,
                WindowNumber = windowNumber,
                WaitingCount = tracking.WaitingCount,
                CompletedCount = tracking.CompletedCount,
                TotalBookings = tracking.TotalBookings,
                Date = tracking.Date.ToString("yyyy-MM-dd"),
                IsActive = tracking.IsActive,
                LastRecallTime = tracking.LastRecallTime?.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        public async Task<QueueTracking> UpdateCurrentServingAsync(int queueNumber, int? windowId = null, bool forceRecall = false)
        {
            var tracking = await GetTodayTrackingAsync();

            if (forceRecall && tracking.CurrentServingNumber == queueNumber)
            {
                // Force update by temporarily changing value
                tracking.CurrentServingNumber = 0;
                await _context.SaveChangesAsync();
            }

            tracking.CurrentServingNumber = queueNumber;
            tracking.LastRecallTime = forceRecall ? DateTime.Now : tracking.LastRecallTime;

            await _context.SaveChangesAsync();
            await RefreshStatisticsAsync(tracking);

            return tracking;
        }

        public async Task<QueueTracking> GetTodayTrackingAsync()
        {
            var today = DateTime.Today;
            var tracking = await _context.QueueTrackings
                .FirstOrDefaultAsync(t => t.Date == today);

            if (tracking == null)
            {
                tracking = new QueueTracking
                {
                    Date = today,
                    CurrentServingNumber = 0,
                    IsActive = true
                };
                _context.QueueTrackings.Add(tracking);
                await _context.SaveChangesAsync();
            }

            return tracking;
        }

        private async Task RefreshStatisticsAsync(QueueTracking tracking)
        {
            var dateStart = tracking.Date;
            var dateEnd = dateStart.AddDays(1);

            tracking.TotalBookings = await _context.QueueBookings
                .CountAsync(b => b.BookingDate >= dateStart && b.BookingDate < dateEnd);

            tracking.WaitingCount = await _context.QueueBookings
                .CountAsync(b => b.BookingDate >= dateStart && 
                                b.BookingDate < dateEnd && 
                                b.Status == "waiting");

            tracking.CompletedCount = await _context.QueueBookings
                .CountAsync(b => b.BookingDate >= dateStart && 
                                b.BookingDate < dateEnd && 
                                b.Status == "completed");

            await _context.SaveChangesAsync();
        }
    }
}

