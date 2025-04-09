using Microsoft.AspNetCore.Mvc;
using Stock_Ease.Services;
using System;
using System.Linq;

namespace Stock_Ease.Controllers
{
    public class SensorsController : Controller
    {
        private readonly IWeightSensorStatusService _sensorStatusService;

        public SensorsController(IWeightSensorStatusService sensorStatusService)
        {
             _sensorStatusService = sensorStatusService;
        }

        public IActionResult Index()
        {
            // Timeout for considering a sensor "active"
            var timeout = TimeSpan.FromMinutes(5);
            var activeSensors = _sensorStatusService.GetActiveSensors(timeout);

            return View(activeSensors);
        }
    }
}
