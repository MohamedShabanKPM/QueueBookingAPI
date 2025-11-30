using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueBookingAPI.DTOs;
using QueueBookingAPI.Services;

namespace QueueBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AuthService authService, ILogger<UsersController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                var userDtos = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                }).ToList();

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new { error = "An error occurred while getting users" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _authService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user");
                return StatusCode(500, new { error = "An error occurred while getting user" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                var user = await _authService.CreateUserAsync(dto.Name, dto.Email, dto.Password, dto.Role);
                
                return Ok(new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { error = "An error occurred while creating user" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var user = await _authService.UpdateUserAsync(id, dto.Name, dto.Email, dto.Password, dto.Role);
                
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new { error = "An error occurred while updating user" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var deleted = await _authService.DeleteUserAsync(id);
                
                if (!deleted)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { error = "An error occurred while deleting user" });
            }
        }
    }
}

