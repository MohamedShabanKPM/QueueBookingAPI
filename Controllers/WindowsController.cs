using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueBookingAPI.Data;
using QueueBookingAPI.Models;

namespace QueueBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WindowsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WindowsController> _logger;

        public WindowsController(ApplicationDbContext context, ILogger<WindowsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetWindows()
        {
            try
            {
                var windows = await _context.QueueWindows
                    .Where(w => w.IsActive)
                    .OrderBy(w => w.Number)
                    .ToListAsync();

                return Ok(windows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting windows");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateWindow([FromBody] QueueWindow window)
        {
            try
            {
                _context.QueueWindows.Add(window);
                await _context.SaveChangesAsync();
                return Ok(window);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating window");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignWindow([FromQuery] int userId, [FromQuery] int windowId)
        {
            try
            {
                // Deactivate other assignments for this user
                var userAssignments = await _context.QueueUserWindows
                    .Where(a => a.UserId == userId && a.IsActive)
                    .ToListAsync();

                foreach (var assignment in userAssignments)
                {
                    assignment.IsActive = false;
                    assignment.ReleaseTime = DateTime.Now;
                }

                // Deactivate other assignments for this window
                var windowAssignments = await _context.QueueUserWindows
                    .Where(a => a.WindowId == windowId && a.IsActive)
                    .ToListAsync();

                foreach (var assignment in windowAssignments)
                {
                    assignment.IsActive = false;
                    assignment.ReleaseTime = DateTime.Now;
                }

                // Create new assignment
                var newAssignment = new QueueUserWindow
                {
                    UserId = userId,
                    WindowId = windowId,
                    IsActive = true,
                    AssignTime = DateTime.Now
                };

                _context.QueueUserWindows.Add(newAssignment);
                await _context.SaveChangesAsync();

                return Ok(newAssignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning window");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserWindow(int userId)
        {
            try
            {
                var assignment = await _context.QueueUserWindows
                    .Include(a => a.Window)
                    .Where(a => a.UserId == userId && a.IsActive)
                    .OrderByDescending(a => a.AssignTime)
                    .FirstOrDefaultAsync();

                if (assignment == null)
                    return NotFound();

                return Ok(assignment.Window);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user window");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }
    }
}

