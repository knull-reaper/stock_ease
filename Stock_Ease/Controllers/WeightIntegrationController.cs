using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stock_Ease.Data;
using Stock_Ease.Models;
using Stock_Ease.Services; // Add this
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stock_Ease.Controllers
{
    // Define a DTO for the incoming request
    public class WeightUpdateRequest
    {
        public int ProductId { get; set; } // Assuming Python script sends ProductId
        public double NewWeight { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class WeightIntegrationController : ControllerBase
    {
        private readonly Stock_EaseContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWeightSensorStatusService _sensorStatusService; // Inject the service

        // TODO: Create a dedicated Discord notification service later
        // private readonly IDiscordNotificationService _discordNotifier;

        public WeightIntegrationController(
            Stock_EaseContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IWeightSensorStatusService sensorStatusService) // Add service to constructor
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _sensorStatusService = sensorStatusService; // Assign injected service
        }

        // --- Endpoint for Python script to update weight ---
        [HttpPost("updateweight")]
        public async Task<IActionResult> UpdateWeight([FromBody] WeightUpdateRequest request, [FromHeader(Name = "X-API-Key")] string apiKey)
        {
            // --- API Key Authentication ---
            var expectedApiKey = _configuration["WeightIntegration:ApiKey"];
            if (string.IsNullOrEmpty(expectedApiKey) || apiKey != expectedApiKey)
            {
                return Unauthorized("Invalid API Key.");
            }

            // --- Find Product ---
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound($"Product with ID {request.ProductId} not found.");
            }

            // --- Update Weight ---
            product.CurrentWeight = request.NewWeight;
            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // --- Check Threshold and Send Alert ---
                if (product.CurrentWeight < product.MinimumThreshold)
                {
                    // TODO: Refactor this into a dedicated service
                    await SendDiscordAlert(product);
                }

                return Ok($"Weight for product {product.Name} (ID: {product.ProductId}) updated to {product.CurrentWeight}.");
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle potential concurrency issues if needed
                return Conflict("Database concurrency conflict occurred.");
            }
            catch (Exception ex)
            {
                // Log the exception (implementation needed)
                Console.WriteLine($"Error updating weight: {ex.Message}"); // Basic console logging
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        // --- Endpoint for Python script to send heartbeat ---
        [HttpPost("heartbeat/{sensorId}")]
        public IActionResult Heartbeat(string sensorId)
        {
            if (string.IsNullOrWhiteSpace(sensorId))
            {
                return BadRequest("Sensor ID cannot be empty.");
            }

            // Record the heartbeat using the injected service
            _sensorStatusService.RecordHeartbeat(sensorId);
            // Console.WriteLine($"Heartbeat received from sensor: {sensorId}"); // Optional logging
            return Ok($"Heartbeat recorded for sensor {sensorId}.");
        }

        // --- Endpoint for Frontend to get active sensors ---
        [HttpGet("sensors")]
        public IActionResult GetActiveSensors()
        {
            // Define a reasonable timeout (e.g., 5 minutes)
            // Consider making this configurable later
            var timeout = TimeSpan.FromMinutes(5);
            var activeSensors = _sensorStatusService.GetActiveSensors(timeout);
            return Ok(activeSensors); // Return the list of active sensors
        }


        // --- Temporary Discord Alert Method (Refactor later) ---
        private async Task SendDiscordAlert(Product product)
        {
            var webhookUrl = _configuration["Discord:WebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Console.WriteLine("Discord Webhook URL not configured. Skipping alert.");
                return; // Don't fail if webhook isn't set
            }

            var message = $"ALERT: Product '{product.Name}' (ID: {product.ProductId}) weight ({product.CurrentWeight}) is below threshold ({product.MinimumThreshold})!";
            var payload = new { content = message };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync(webhookUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error sending Discord alert: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
                else
                {
                     Console.WriteLine($"Discord alert sent successfully for Product ID: {product.ProductId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception sending Discord alert: {ex.Message}");
            }
        }
    }
}
