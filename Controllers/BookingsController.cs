using Microsoft.AspNetCore.Mvc;
using QueueBookingAPI.DTOs;
using QueueBookingAPI.Services;

namespace QueueBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly QueueBookingService _bookingService;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(QueueBookingService bookingService, ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Phone))
                {
                    return BadRequest(new { error = "Name and phone are required" });
                }

                if (dto.Name.Length < 2)
                {
                    return BadRequest(new { error = "Name must be at least 2 characters" });
                }

                if (dto.Phone.Length < 10)
                {
                    return BadRequest(new { error = "Phone must be at least 10 digits" });
                }

                var booking = await _bookingService.CreateBookingAsync(dto);

                var response = new BookingResponseDto
                {
                    Id = booking.Id,
                    Name = booking.Name,
                    Phone = booking.Phone,
                    Email = booking.Email,
                    BookingDate = booking.BookingDate,
                    QueueNumber = booking.QueueNumber,
                    Status = booking.Status,
                    WindowNumber = booking.Window?.Number,
                    DisplayName = booking.DisplayName
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return StatusCode(500, new { error = "An error occurred while creating the booking" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBookings([FromQuery] DateTime? date, [FromQuery] string? status)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsAsync(date, status);
                var response = bookings.Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Phone = b.Phone,
                    Email = b.Email,
                    BookingDate = b.BookingDate,
                    QueueNumber = b.QueueNumber,
                    Status = b.Status,
                    WindowNumber = b.Window?.Number,
                    DisplayName = b.DisplayName
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings");
                return StatusCode(500, new { error = "An error occurred while fetching bookings" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsAsync();
                var booking = bookings.FirstOrDefault(b => b.Id == id);

                if (booking == null)
                    return NotFound();

                var response = new BookingResponseDto
                {
                    Id = booking.Id,
                    Name = booking.Name,
                    Phone = booking.Phone,
                    Email = booking.Email,
                    BookingDate = booking.BookingDate,
                    QueueNumber = booking.QueueNumber,
                    Status = booking.Status,
                    WindowNumber = booking.Window?.Number,
                    DisplayName = booking.DisplayName
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking");
                return StatusCode(500, new { error = "An error occurred while fetching booking" });
            }
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartProcessing(int id, [FromQuery] int userId)
        {
            try
            {
                var booking = await _bookingService.StartProcessingAsync(id, userId);
                if (booking == null)
                    return NotFound();

                return Ok(new { message = "Processing started", bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting processing");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            try
            {
                var booking = await _bookingService.CompleteBookingAsync(id);
                if (booking == null)
                    return NotFound();

                return Ok(new { message = "Booking completed", bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing booking");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromQuery] int userId)
        {
            try
            {
                var booking = await _bookingService.CancelBookingAsync(id, userId);
                if (booking == null)
                    return NotFound();

                return Ok(new { message = "Booking cancelled", bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpPost("next-waiting")]
        public async Task<IActionResult> GetNextWaiting([FromQuery] int? userId)
        {
            try
            {
                var booking = await _bookingService.GetNextWaitingReservationAsync(userId);
                if (booking == null)
                    return NotFound(new { message = "No waiting reservations" });

                var response = new BookingResponseDto
                {
                    Id = booking.Id,
                    Name = booking.Name,
                    Phone = booking.Phone,
                    Email = booking.Email,
                    BookingDate = booking.BookingDate,
                    QueueNumber = booking.QueueNumber,
                    Status = booking.Status,
                    WindowNumber = booking.Window?.Number,
                    DisplayName = booking.DisplayName
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next waiting");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] DateTime? date)
        {
            try
            {
                var stats = await _bookingService.GetDashboardStatisticsAsync(date);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }
    }
}

