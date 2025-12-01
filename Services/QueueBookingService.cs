using Microsoft.EntityFrameworkCore;
using QueueBookingAPI.Data;
using QueueBookingAPI.Models;
using QueueBookingAPI.DTOs;

namespace QueueBookingAPI.Services
{
    public class QueueBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QueueBookingService> _logger;

        public QueueBookingService(ApplicationDbContext context, ILogger<QueueBookingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<QueueBooking> CreateBookingAsync(BookingDto dto)
        {
            // Set booking date
            DateTime bookingDate;
            if (dto.BookingDateSelection == "tomorrow")
            {
                var tomorrow = DateTime.Today.AddDays(1);
                bookingDate = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 9, 0, 0);
            }
            else
            {
                bookingDate = DateTime.Now;
            }

            // Check for duplicate phone on same day
            var dateStart = bookingDate.Date;
            var dateEnd = dateStart.AddDays(1);

            var existing = await _context.QueueBookings
                .Where(b => b.Phone == dto.Phone && 
                           b.BookingDate >= dateStart && 
                           b.BookingDate < dateEnd)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                throw new InvalidOperationException($"Phone number {dto.Phone} already has a booking for this date.");
            }

            // Get max queue number atomically
            // Check if any bookings exist for this date
            var hasBookings = await _context.QueueBookings
                .AnyAsync(b => b.BookingDate >= dateStart && b.BookingDate < dateEnd);
            
            int nextQueueNumber;
            if (hasBookings)
            {
                var maxQueueNumber = await _context.QueueBookings
                    .Where(b => b.BookingDate >= dateStart && b.BookingDate < dateEnd)
                    .MaxAsync(b => b.QueueNumber);
                nextQueueNumber = maxQueueNumber + 1;
            }
            else
            {
                nextQueueNumber = 1;
            }

            var booking = new QueueBooking
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                BookingDate = bookingDate,
                BookingDateSelection = dto.BookingDateSelection,
                QueueNumber = nextQueueNumber,
                Status = "waiting"
            };

            _context.QueueBookings.Add(booking);
            await _context.SaveChangesAsync();

            // Refresh tracking statistics
            await RefreshTrackingStatisticsAsync();

            return booking;
        }

        public async Task<QueueBooking?> GetNextWaitingReservationAsync(int? userId = null)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // First try to get waiting without window
            var waiting = await _context.QueueBookings
                .Include(b => b.Window)
                .Include(b => b.StartedByUser)
                .Where(b => b.Status == "waiting" &&
                           b.BookingDate >= today &&
                           b.BookingDate < tomorrow &&
                           b.WindowId == null)
                .OrderBy(b => b.QueueNumber)
                .FirstOrDefaultAsync();

            // If none, get any waiting
            if (waiting == null)
            {
                waiting = await _context.QueueBookings
                    .Include(b => b.Window)
                    .Include(b => b.StartedByUser)
                    .Where(b => b.Status == "waiting" &&
                               b.BookingDate >= today &&
                               b.BookingDate < tomorrow)
                    .OrderBy(b => b.QueueNumber)
                    .FirstOrDefaultAsync();
            }

            if (waiting != null && userId.HasValue)
            {
                // Assign window to booking
                var userWindow = await GetUserWindowAsync(userId.Value);
                if (userWindow != null)
                {
                    waiting.WindowId = userWindow.WindowId;
                    await _context.SaveChangesAsync();
                }

                // Update tracking
                var tracking = await GetTodayTrackingAsync();
                await UpdateCurrentServingAsync(tracking, waiting.QueueNumber, waiting.WindowId);
            }

            return waiting;
        }

        public async Task<QueueBooking?> StartProcessingAsync(int bookingId, int userId)
        {
            var booking = await _context.QueueBookings
                .Include(b => b.Window)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.Status != "waiting")
                return null;

            var userWindow = await GetUserWindowAsync(userId);
            if (userWindow != null)
            {
                booking.WindowId = userWindow.WindowId;
            }

            booking.Status = "in_progress";
            booking.ActualStartTime = DateTime.Now;
            booking.StartedByUserId = userId;

            await _context.SaveChangesAsync();
            await RefreshTrackingStatisticsAsync();

            return booking;
        }

        public async Task<QueueBooking?> CompleteBookingAsync(int bookingId)
        {
            var booking = await _context.QueueBookings
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.Status != "in_progress")
                return null;

            booking.Status = "completed";
            booking.ActualEndTime = DateTime.Now;

            // Calculate time taken
            if (booking.ActualStartTime.HasValue && booking.ActualEndTime.HasValue)
            {
                var timeDiff = booking.ActualEndTime.Value - booking.ActualStartTime.Value;
                var totalSeconds = (int)timeDiff.TotalSeconds;
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                booking.TimeTaken = $"{minutes:D2}:{seconds:D2}";
            }

            await _context.SaveChangesAsync();
            await RefreshTrackingStatisticsAsync();

            return booking;
        }

        public async Task<QueueBooking?> CancelBookingAsync(int bookingId, int userId)
        {
            var booking = await _context.QueueBookings
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.Status == "completed")
                return null;

            booking.Status = "cancelled";
            booking.StartedByUserId = userId;

            await _context.SaveChangesAsync();
            await RefreshTrackingStatisticsAsync();

            return booking;
        }

        public async Task<QueueBooking?> ResetBookingAsync(int bookingId)
        {
            var booking = await _context.QueueBookings
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return null;

            booking.Status = "waiting";
            booking.ActualStartTime = null;
            booking.ActualEndTime = null;
            booking.StartedByUserId = null;
            booking.TimeTaken = null;

            await _context.SaveChangesAsync();
            await RefreshTrackingStatisticsAsync();

            return booking;
        }

        public async Task<List<QueueBooking>> GetBookingsAsync(DateTime? date = null, string? status = null)
        {
            var query = _context.QueueBookings
                .Include(b => b.Window)
                .Include(b => b.StartedByUser)
                .AsQueryable();

            if (date.HasValue)
            {
                var dateStart = date.Value.Date;
                var dateEnd = dateStart.AddDays(1);
                query = query.Where(b => b.BookingDate >= dateStart && b.BookingDate < dateEnd);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            return await query.OrderBy(b => b.QueueNumber).ToListAsync();
        }

        public async Task<DashboardStatsDto> GetDashboardStatisticsAsync(DateTime? targetDate = null)
        {
            if (!targetDate.HasValue)
                targetDate = DateTime.Today;

            var dateStart = targetDate.Value.Date;
            var dateEnd = dateStart.AddDays(1);

            var bookings = await _context.QueueBookings
                .Include(b => b.StartedByUser)
                .Where(b => b.BookingDate >= dateStart && b.BookingDate < dateEnd)
                .ToListAsync();

            var stats = new DashboardStatsDto
            {
                TotalToday = bookings.Count,
                Waiting = bookings.Count(b => b.Status == "waiting"),
                InProgress = bookings.Count(b => b.Status == "in_progress"),
                Completed = bookings.Count(b => b.Status == "completed"),
                Cancelled = bookings.Count(b => b.Status == "cancelled")
            };

            // Employee statistics
            var employeeStats = bookings
                .Where(b => b.StartedByUserId.HasValue)
                .GroupBy(b => new { b.StartedByUserId, b.StartedByUser!.Name })
                .Select(g => new
                {
                    UserId = g.Key.StartedByUserId!.Value,
                    Name = g.Key.Name,
                    Bookings = g.ToList()
                })
                .ToList();

            foreach (var emp in employeeStats)
            {
                var completed = emp.Bookings.Where(b => b.Status == "completed" && 
                    b.ActualStartTime.HasValue && b.ActualEndTime.HasValue).ToList();
                
                var timeDeltas = completed
                    .Select(b => (int)(b.ActualEndTime!.Value - b.ActualStartTime!.Value).TotalSeconds)
                    .ToList();

                var empStat = new EmployeeStatsDto
                {
                    Name = emp.Name,
                    Count = emp.Bookings.Count,
                    Completed = emp.Bookings.Count(b => b.Status == "completed"),
                    Cancelled = emp.Bookings.Count(b => b.Status == "cancelled")
                };

                if (timeDeltas.Any())
                {
                    empStat.AverageTime = FormatTime((int)timeDeltas.Average());
                    empStat.MinTime = FormatTime(timeDeltas.Min());
                    empStat.MaxTime = FormatTime(timeDeltas.Max());
                }

                stats.EmployeeStats.Add(empStat);
            }

            stats.EmployeeStats = stats.EmployeeStats.OrderByDescending(e => e.Count).ToList();

            // Time statistics
            var completedWithTime = bookings
                .Where(b => b.Status == "completed" && 
                           b.ActualStartTime.HasValue && 
                           b.ActualEndTime.HasValue)
                .ToList();

            if (completedWithTime.Any())
            {
                var timeDeltas = completedWithTime
                    .Select(b => (int)(b.ActualEndTime!.Value - b.ActualStartTime!.Value).TotalSeconds)
                    .ToList();

                stats.TimeStats = new TimeStatsDto
                {
                    Average = FormatTime((int)timeDeltas.Average()),
                    Min = FormatTime(timeDeltas.Min()),
                    Max = FormatTime(timeDeltas.Max()),
                    TotalCompleted = completedWithTime.Count
                };
            }
            else
            {
                stats.TimeStats = new TimeStatsDto();
            }

            return stats;
        }

        private string FormatTime(int seconds)
        {
            var minutes = seconds / 60;
            var secs = seconds % 60;
            return $"{minutes:D2}:{secs:D2}";
        }

        private async Task<QueueUserWindow?> GetUserWindowAsync(int userId)
        {
            return await _context.QueueUserWindows
                .Include(u => u.Window)
                .Where(u => u.UserId == userId && u.IsActive)
                .OrderByDescending(u => u.AssignTime)
                .FirstOrDefaultAsync();
        }

        public async Task<QueueBooking?> UpdateBookingWindowAsync(int bookingId, int? windowId)
        {
            var booking = await _context.QueueBookings
                .Include(b => b.Window)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return null;

            booking.WindowId = windowId;
            await _context.SaveChangesAsync();

            return booking;
        }

        private async Task<QueueTracking> GetTodayTrackingAsync()
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

        private async Task UpdateCurrentServingAsync(QueueTracking tracking, int queueNumber, int? windowId = null)
        {
            tracking.CurrentServingNumber = queueNumber;
            await _context.SaveChangesAsync();
        }

        private async Task RefreshTrackingStatisticsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var tracking = await GetTodayTrackingAsync();

            tracking.TotalBookings = await _context.QueueBookings
                .CountAsync(b => b.BookingDate >= today && b.BookingDate < tomorrow);

            tracking.WaitingCount = await _context.QueueBookings
                .CountAsync(b => b.BookingDate >= today && 
                                b.BookingDate < tomorrow && 
                                b.Status == "waiting");

            tracking.CompletedCount = await _context.QueueBookings
                .CountAsync(b => b.BookingDate >= today && 
                                b.BookingDate < tomorrow && 
                                b.Status == "completed");

            await _context.SaveChangesAsync();
        }
    }
}

