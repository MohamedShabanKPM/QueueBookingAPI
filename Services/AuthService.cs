using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QueueBookingAPI.Data;
using QueueBookingAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace QueueBookingAPI.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            return GenerateJwtToken(user);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role) // Add role claim with short name for JWT compatibility
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "QueueBookingAPI",
                audience: _configuration["Jwt:Audience"] ?? "QueueBookingClient",
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User> CreateUserAsync(string name, string email, string password, string role = "User")
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<User?> UpdateUserAsync(int userId, string? name, string? email, string? password, string? role)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(name))
            {
                user.Name = name;
            }

            if (!string.IsNullOrEmpty(email))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.Id != userId);
                
                if (existingUser != null)
                {
                    throw new InvalidOperationException("User with this email already exists");
                }
                
                user.Email = email;
            }

            if (!string.IsNullOrEmpty(password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }

            if (!string.IsNullOrEmpty(role))
            {
                user.Role = role;
            }

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

