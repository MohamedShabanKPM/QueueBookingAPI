using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueBookingAPI.Data;
using QueueBookingAPI.Models;

namespace QueueBookingAPI.Controllers
{
    public class CreateWindowDto
    {
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateWindowDto
    {
        public string? Name { get; set; }
        public int? Number { get; set; }
        public bool? IsActive { get; set; }
    }

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
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> CreateWindow([FromBody] CreateWindowDto dto)
        {
            try
            {
                // Check if window number already exists
                var existing = await _context.QueueWindows
                    .FirstOrDefaultAsync(w => w.Number == dto.Number);
                
                if (existing != null)
                {
                    return BadRequest(new { error = $"Window number {dto.Number} already exists" });
                }

                var window = new QueueWindow
                {
                    Name = dto.Name,
                    Number = dto.Number,
                    IsActive = dto.IsActive
                };

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

        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateWindow(int id, [FromBody] UpdateWindowDto dto)
        {
            try
            {
                var window = await _context.QueueWindows.FindAsync(id);
                if (window == null)
                    return NotFound();

                // Check if window number already exists (if changed)
                if (dto.Number.HasValue && dto.Number.Value != window.Number)
                {
                    var existing = await _context.QueueWindows
                        .FirstOrDefaultAsync(w => w.Number == dto.Number.Value && w.Id != id);
                    
                    if (existing != null)
                    {
                        return BadRequest(new { error = $"Window number {dto.Number.Value} already exists" });
                    }
                    window.Number = dto.Number.Value;
                }

                if (!string.IsNullOrEmpty(dto.Name))
                    window.Name = dto.Name;
                
                if (dto.IsActive.HasValue)
                    window.IsActive = dto.IsActive.Value;

                await _context.SaveChangesAsync();
                return Ok(window);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating window");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpGet("{id}/current-user")]
        public async Task<IActionResult> GetCurrentUser(int id)
        {
            try
            {
                var assignment = await _context.QueueUserWindows
                    .Include(a => a.User)
                    .Where(a => a.WindowId == id && a.IsActive)
                    .OrderByDescending(a => a.AssignTime)
                    .FirstOrDefaultAsync();

                if (assignment == null)
                    return NotFound();

                return Ok(new { id = assignment.User.Id, name = assignment.User.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
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

