using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stock_Ease.Data;
using Stock_Ease.Hubs;
using Stock_Ease.Models;
using Stock_Ease.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stock_Ease.Controllers
{
    public class WeightUpdateRequest // DTO for weight update requests
    {
        public int ProductId { get; set; } // Expected from Python script
        public double NewWeight { get; set; }
    }

    public class ScreenDataRequest // DTO for screen data requests
    {
        public string? SensorId { get; set; } // Sensor identifier
        public double Value { get; set; }    // Value read from screen/mocked
    }

    [Route("api/[controller]")]
    [ApiController]
    public class WeightIntegrationController : ControllerBase
    {
        private readonly Stock_EaseContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWeightSensorStatusService _sensorStatusService;
        private readonly IHubContext<TransactionHub> _hubContext;
        private readonly AlertsController _alertsController;
        private static readonly TimeSpan MISSING_PRODUCT_DELAY = TimeSpan.FromMinutes(10); // Delay for missing product alert

        // TODO: Create a dedicated Discord notification service later
        // private readonly IDiscordNotificationService _discordNotifier;

        public WeightIntegrationController(
            Stock_EaseContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IWeightSensorStatusService sensorStatusService,
            IHubContext<TransactionHub> hubContext,
            AlertsController alertsController)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _sensorStatusService = sensorStatusService;
            _hubContext = hubContext;
            _alertsController = alertsController;
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
                // Handle concurrency issues
                return Conflict("Database concurrency conflict occurred.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating weight: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        // --- REMOVED Heartbeat Endpoint ---
        // The ReceiveScreenData endpoint now implicitly handles heartbeats 
        // by calling RecordSensorUpdate in the service.

        // --- Endpoint for Frontend to get active sensors ---
        [HttpGet("sensors")]
        public IActionResult GetActiveSensors()
        {
            var timeout = TimeSpan.FromMinutes(5); // Consider making configurable
            var activeSensors = _sensorStatusService.GetActiveSensors(timeout);
            return Ok(activeSensors);
        }

        // --- Endpoint for Python OCR script to send screen data ---
        [HttpPost("screendata")]
        public async Task<IActionResult> ReceiveScreenData([FromBody] ScreenDataRequest request, [FromHeader(Name = "X-API-Key")] string apiKey)
        {
            // --- API Key Authentication ---
            var expectedApiKey = _configuration["WeightIntegration:ApiKey"];
            if (string.IsNullOrEmpty(expectedApiKey) || apiKey != expectedApiKey)
            {
                Console.WriteLine($"Unauthorized attempt to access screendata endpoint. Provided Key: '{apiKey}'");
                return Unauthorized("Invalid API Key.");
            }

            // --- Validate Input ---
            if (string.IsNullOrWhiteSpace(request.SensorId))
            {
                return BadRequest("SensorId is required.");
            }

            // --- Record Sensor Update & Handle Timer Logic ---
            var updateResult = _sensorStatusService.RecordSensorUpdate(request.SensorId, request.Value);
            Console.WriteLine($"Sensor update recorded for {request.SensorId}. Weight: {request.Value}. Result: {updateResult}");

            // --- Find Product by SensorId ---
            var product = await _context.Products
                                        .FirstOrDefaultAsync(p => p.SensorId == request.SensorId);

            if (product == null)
            {
                Console.WriteLine($"Received screen data for SensorId '{request.SensorId}' (Value: {request.Value}), but no matching product found.");
                // Request is valid, but no product action taken
                return Ok($"Data received for SensorId '{request.SensorId}', but no product is linked to this sensor.");
            }

            // --- Update Product Weight ---
            product.CurrentWeight = request.Value;
            _context.Entry(product).State = EntityState.Modified;

            try
            {
                // --- Handle Alerts based on Sensor Update Result ---
                // (Alerts are added to context within the switch/if blocks below)
                bool sendNormalLowStockAlert = false; 

                switch (updateResult)
                {
                    case SensorUpdateResult.RestockingTimerStarted:
                        // Timer started, suppress immediate low stock alert
                        Console.WriteLine($"Restocking timer started for {product.Name}, suppressing immediate low stock alert.");
                        break;

                    case SensorUpdateResult.RestockingTimerCancelled:
                        // Timer cancelled, check normal threshold now
                        sendNormalLowStockAlert = true;
                        break;

                    case SensorUpdateResult.MissingProductAlertNeeded:
                        {
                            // Timer expired, create "missing product" alert if needed
                            string missingMsg = $"Product '{product.Name}' (ID: {product.ProductId}, Sensor: {product.SensorId}) appears to be MISSING! Weight remained near zero for over {MISSING_PRODUCT_DELAY.TotalMinutes} minutes.";
                            bool missingAlertExists = await _context.Alerts
                                .AnyAsync(a => a.ProductId == product.ProductId && !a.IsRead && a.Message.Contains("appears to be MISSING")); // Check for phrase

                            if (!missingAlertExists)
                            {
                                Console.WriteLine($"Attempting to add MISSING PRODUCT alert to context for Product ID: {product.ProductId}. Message: {missingMsg}");
                                var missingAlert = new Alert { ProductId = product.ProductId, Message = missingMsg, AlertDate = DateTime.UtcNow, IsRead = false };
                                _context.Alerts.Add(missingAlert);
                                await SendDiscordAlert(product, isMissingAlert: true);
                            }
                            else
                            {
                                Console.WriteLine($"Similar unread MISSING PRODUCT alert already exists for Product ID: {product.ProductId}. Skipping creation.");
                            }
                            break;
                        }
                    case SensorUpdateResult.Ok:
                    default:
                         // Normal update, check threshold
                        sendNormalLowStockAlert = true;
                        break;
                }

                // Check normal low stock threshold if applicable and not overridden by timer logic
                if (sendNormalLowStockAlert && product.ThresholdType == "Weight" && product.CurrentWeight < product.MinimumThreshold)
                {
                    Console.WriteLine($"Creating Weight threshold alert for Product ID: {product.ProductId}. Current: {product.CurrentWeight}, Threshold: {product.MinimumThreshold}");
                    string lowWeightMsg = $"Product '{product.Name}' (ID: {product.ProductId}) weight ({product.CurrentWeight:F2}) is below threshold ({product.MinimumThreshold}).";
                    // Check for existing unread low weight alert (by phrase)
                    bool alertExists = await _context.Alerts
                        .AnyAsync(a => a.ProductId == product.ProductId && !a.IsRead && a.Message.Contains("is below threshold")); // Check for phrase

                    if (!alertExists)
                    {
                        var lowWeightAlert = new Alert { ProductId = product.ProductId, Message = lowWeightMsg, AlertDate = DateTime.UtcNow, IsRead = false };
                        _context.Alerts.Add(lowWeightAlert); // Add DB alert
                        await SendDiscordAlert(product, isMissingAlert: false); // Send Discord notification
                    }
                    else
                    {
                         Console.WriteLine($"Similar unread weight alert already exists for Product ID: {product.ProductId}. Skipping creation.");
                    }
                }
                else if (sendNormalLowStockAlert && product.ThresholdType == "Weight")
                {
                 Console.WriteLine($"Weight threshold NOT triggered for Product ID: {product.ProductId}. Current: {product.CurrentWeight}, Threshold: {product.MinimumThreshold}");
                }
                // --- End Alert Handling ---

                // --- Save Changes (Product weight update AND any added alerts) ---
                Console.WriteLine($"Calling SaveChangesAsync. Context has {_context.ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged)} pending changes.");
                int changes = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChangesAsync completed. {changes} state entries written to the database.");
                // --- End Save Changes ---

                // --- Send SignalR Notification ---
                await _hubContext.Clients.All.SendAsync("ReceiveWeightUpdate", product.ProductId, product.CurrentWeight);
                Console.WriteLine($"Sent SignalR weight update for Product ID: {product.ProductId}");
                // --- End SignalR Notification ---

                return Ok($"Weight for product '{product.Name}' (linked to SensorId '{request.SensorId}') updated to {product.CurrentWeight}.");
            }
            catch (DbUpdateConcurrencyException ex) // Specific exception
            {
                return Conflict("Database concurrency conflict occurred while updating product weight.");
            }
            catch (Exception ex) // General exceptions
            {
                Console.WriteLine($"Error processing screendata or saving changes: {ex.Message}");
                if (ex.InnerException != null) // Log inner exception
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, "An internal server error occurred while processing sensor data.");
            }
        }


        // --- Temporary Discord Alert Method (Refactor later) ---
        private async Task SendDiscordAlert(Product product, bool isMissingAlert = false) // Flag customizes message
        {
            var webhookUrl = _configuration["Discord:WebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Console.WriteLine("Discord Webhook URL not configured. Skipping alert.");
                return; // Don't fail if not configured
            }

            string message;
            if (isMissingAlert)
            {
                 message = $"ALERT: Product '{product.Name}' (ID: {product.ProductId}, Sensor: {product.SensorId}) appears to be MISSING! Weight remained near zero for over {MISSING_PRODUCT_DELAY.TotalMinutes} minutes.";
            }
            else
            {
                 message = $"ALERT: Product '{product.Name}' (ID: {product.ProductId}) weight ({product.CurrentWeight}) is below threshold ({product.MinimumThreshold})!";
            }

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
