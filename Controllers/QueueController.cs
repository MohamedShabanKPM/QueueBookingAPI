using Microsoft.AspNetCore.Mvc;
using QueueBookingAPI.Services;

namespace QueueBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public class QueueController : ControllerBase
    {
        private readonly QueueTrackingService _trackingService;
        private readonly ILogger<QueueController> _logger;

        public QueueController(QueueTrackingService trackingService, ILogger<QueueController> logger)
        {
            _trackingService = trackingService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var status = await _trackingService.GetQueueStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpPost("update-serving")]
        public async Task<IActionResult> UpdateServing([FromQuery] int queueNumber, [FromQuery] int? windowId, [FromQuery] bool forceRecall = false)
        {
            try
            {
                var tracking = await _trackingService.UpdateCurrentServingAsync(queueNumber, windowId, forceRecall);
                return Ok(new { message = "Updated", currentServing = tracking.CurrentServingNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating serving");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }
    }
}

