using Microsoft.AspNetCore.Mvc;
using QueueBookingAPI.Services;

namespace QueueBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SetupController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<SetupController> _logger;

        public SetupController(AuthService authService, ILogger<SetupController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto dto)
        {
            try
            {
                var user = await _authService.CreateUserAsync(
                    dto.Name ?? "Admin",
                    dto.Email,
                    dto.Password,
                    "Admin"
                );

                return Ok(new
                {
                    message = "Admin user created successfully",
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        role = user.Role
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin user");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }
    }

    public class CreateAdminDto
    {
        public string? Name { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

