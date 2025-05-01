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
    public class WeightUpdateRequest
    {
        public int ProductId
        {
            get;
            set;
        }
        public double NewWeight
        {
            get;
            set;
        }
    }

    public class ScreenDataRequest
    {
        public string? SensorId
        {
            get;
            set;
        }
        public double Value
        {
            get;
            set;
        }
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
        private static readonly TimeSpan MISSING_PRODUCT_DELAY = TimeSpan.FromMinutes(10);

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

        [HttpPost("updateweight")]
        public async Task<IActionResult> UpdateWeight([FromBody] WeightUpdateRequest request, [FromHeader(Name = "X-API-Key")] string apiKey)
        {

            var expectedApiKey = _configuration["WeightIntegration:ApiKey"];
            if (string.IsNullOrEmpty(expectedApiKey) || apiKey != expectedApiKey)
            {
                return Unauthorized("Invalid API Key.");
            }

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound($"Product with ID {request.ProductId} not found.");
            }

            product.CurrentWeight = request.NewWeight;
            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                if (product.CurrentWeight < product.MinimumThreshold)
                {

                    await SendDiscordAlert(product);
                }

                return Ok($"Weight for product {product.Name} (ID: {product.ProductId}) updated to {product.CurrentWeight}.");
            }
            catch (DbUpdateConcurrencyException)
            {

                return Conflict("Database concurrency conflict occurred.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating weight: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpGet("sensors")]
        public IActionResult GetActiveSensors()
        {
            var timeout = TimeSpan.FromMinutes(5);
            var activeSensors = _sensorStatusService.GetActiveSensors(timeout);
            return Ok(activeSensors);
        }

        [HttpPost("screendata")]
        public async Task<IActionResult> ReceiveScreenData([FromBody] ScreenDataRequest request, [FromHeader(Name = "X-API-Key")] string apiKey)
        {
            var expectedApiKey = _configuration["WeightIntegration:ApiKey"];
            if (string.IsNullOrEmpty(expectedApiKey) || apiKey != expectedApiKey)
            {
                Console.WriteLine($"Unauthorized attempt to access screendata endpoint. Provided Key: '{apiKey}'");
                return Unauthorized("Invalid API Key.");
            }

            if (string.IsNullOrWhiteSpace(request.SensorId))
            {
                return BadRequest("SensorId is required.");
            }

            var updateResult = _sensorStatusService.RecordSensorUpdate(request.SensorId, request.Value);
            Console.WriteLine($"Sensor update recorded for {request.SensorId}. Weight: {request.Value}. Result: {updateResult}");

            var product = await _context.Products
              .FirstOrDefaultAsync(p => p.SensorId == request.SensorId);

            if (product == null)
            {
                Console.WriteLine($"Received screen data for SensorId '{request.SensorId}' (Value: {request.Value}), but no matching product found.");

                return Ok($"Data received for SensorId '{request.SensorId}', but no product is linked to this sensor.");
            }

            product.CurrentWeight = request.Value;
            _context.Entry(product).State = EntityState.Modified;

            try
            {

                bool sendNormalLowStockAlert = false;

                switch (updateResult)
                {
                    case SensorUpdateResult.RestockingTimerStarted:

                        Console.WriteLine($"Restocking timer started for {product.Name}, suppressing immediate low stock alert.");
                        break;

                    case SensorUpdateResult.RestockingTimerCancelled:

                        sendNormalLowStockAlert = true;
                        break;

                    case SensorUpdateResult.MissingProductAlertNeeded:
                        {

                            string missingMsg = $ "Product '{product.Name}' (ID: {product.ProductId}, Sensor: {product.SensorId}) appears to be MISSING! Weight remained near zero for over {MISSING_PRODUCT_DELAY.TotalMinutes} minutes.";
                            bool missingAlertExists = await _context.Alerts
                              .AnyAsync(a => a.ProductId == product.ProductId && !a.IsRead && a.Message.Contains("appears to be MISSING"));

                            if (!missingAlertExists)
                            {
                                Console.WriteLine($"Attempting to add MISSING PRODUCT alert to context for Product ID: {product.ProductId}. Message: {missingMsg}");
                                var missingAlert = new Alert
                                {
                                    ProductId = product.ProductId,
                                    Message = missingMsg,
                                    AlertDate = DateTime.UtcNow,
                                    IsRead = false
                                };
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

                        sendNormalLowStockAlert = true;
                        break;
                }

                if (sendNormalLowStockAlert && product.ThresholdType == "Weight" && product.CurrentWeight < product.MinimumThreshold)
                {
                    Console.WriteLine($"Creating Weight threshold alert for Product ID: {product.ProductId}. Current: {product.CurrentWeight}, Threshold: {product.MinimumThreshold}");
                    string lowWeightMsg = $ "Product '{product.Name}' (ID: {product.ProductId}) weight ({product.CurrentWeight:F2}) is below threshold ({product.MinimumThreshold}).";

                    bool alertExists = await _context.Alerts
                      .AnyAsync(a => a.ProductId == product.ProductId && !a.IsRead && a.Message.Contains("is below threshold"));

                    if (!alertExists)
                    {
                        var lowWeightAlert = new Alert
                        {
                            ProductId = product.ProductId,
                            Message = lowWeightMsg,
                            AlertDate = DateTime.UtcNow,
                            IsRead = false
                        };
                        _context.Alerts.Add(lowWeightAlert);
                        await SendDiscordAlert(product, isMissingAlert: false);
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

                Console.WriteLine($"Calling SaveChangesAsync. Context has {_context.ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged)} pending changes.");
                int changes = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChangesAsync completed. {changes} state entries written to the database.");

                await _hubContext.Clients.All.SendAsync("ReceiveWeightUpdate", product.ProductId, product.CurrentWeight);
                Console.WriteLine($"Sent SignalR weight update for Product ID: {product.ProductId}");

                return Ok($"Weight for product '{product.Name}' (linked to SensorId '{request.SensorId}') updated to {product.CurrentWeight}.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Conflict("Database concurrency conflict occurred while updating product weight.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing screendata or saving changes: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, "An internal server error occurred while processing sensor data.");
            }
        }

        private async Task SendDiscordAlert(Product product, bool isMissingAlert = false)
        {
            var webhookUrl = _configuration["Discord:WebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Console.WriteLine("Discord Webhook URL not configured. Skipping alert.");
                return;
            }

            string message;
            if (isMissingAlert)
            {
                message = $ "ALERT: Product '{product.Name}' (ID: {product.ProductId}, Sensor: {product.SensorId}) appears to be MISSING! Weight remained near zero for over {MISSING_PRODUCT_DELAY.TotalMinutes} minutes.";
            }
            else
            {
                message = $ "ALERT: Product '{product.Name}' (ID: {product.ProductId}) weight ({product.CurrentWeight}) is below threshold ({product.MinimumThreshold})!";
            }

            var payload = new
            {
                content = message
            };
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