using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace QueueBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpPost("test-password")]
        public IActionResult TestPassword([FromBody] TestPasswordDto dto)
        {
            // Test if the same password produces different hashes
            var hash1 = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var hash2 = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var hash3 = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Test if all hashes can verify the same password
            var verify1 = BCrypt.Net.BCrypt.Verify(dto.Password, hash1);
            var verify2 = BCrypt.Net.BCrypt.Verify(dto.Password, hash2);
            var verify3 = BCrypt.Net.BCrypt.Verify(dto.Password, hash3);

            // Test verification with one of your actual hashes
            var testHash = "$2a$11$PHhgRwRxzTePfWntarTdCu/OsORTxUkmblhKyUaBIB/a8LgQ1JbH6";
            var verifyActual = BCrypt.Net.BCrypt.Verify(dto.Password, testHash);

            return Ok(new
            {
                message = "BCrypt Password Hashing Test",
                password = dto.Password,
                explanation = "BCrypt generates different hashes for the same password (this is normal and secure!)",
                hash1 = hash1,
                hash2 = hash2,
                hash3 = hash3,
                allHashesDifferent = hash1 != hash2 && hash2 != hash3 && hash1 != hash3,
                verificationResults = new
                {
                    hash1_verifies = verify1,
                    hash2_verifies = verify2,
                    hash3_verifies = verify3,
                    actualHash_verifies = verifyActual
                },
                note = "All hashes can verify the same password even though they look different!"
            });
        }
    }

    public class TestPasswordDto
    {
        public string Password { get; set; } = string.Empty;
    }
}

