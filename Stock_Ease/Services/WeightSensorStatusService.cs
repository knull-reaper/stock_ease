using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Stock_Ease.Services
{
    public enum SensorUpdateResult
    {
        Ok,
        RestockingTimerStarted,
        RestockingTimerCancelled,
        MissingProductAlertNeeded
    }

    public class SensorStatus
    {
        public string SensorId { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; }
        public double? LastKnownWeight { get; set; }
        public bool IsRestockingTimerActive { get; set; } = false; // Is restocking timer running?
        public DateTime? RestockingTimerStartedUtc { get; set; } // Timer start time (UTC)
    }

    public interface IWeightSensorStatusService
    {
        SensorUpdateResult RecordSensorUpdate(string sensorId, double weight); // Records update, returns status for alert logic
        List<SensorStatus> GetActiveSensors(TimeSpan timeout);
        SensorStatus? GetSensorStatus(string sensorId); // Gets status for a specific sensor
    }

    public class WeightSensorStatusService : IWeightSensorStatusService
    {
        private readonly ConcurrentDictionary<string, SensorStatus> _sensorStatuses = new ConcurrentDictionary<string, SensorStatus>();
        private const double RESTOCKING_WEIGHT_THRESHOLD = 3.0; // Weight threshold to start restocking timer
        private static readonly TimeSpan MISSING_PRODUCT_DELAY = TimeSpan.FromMinutes(10); // Delay for missing product alert

        public SensorUpdateResult RecordSensorUpdate(string sensorId, double weight)
        {
            if (string.IsNullOrWhiteSpace(sensorId))
            {
                // Ignore updates with empty SensorId for now
                return SensorUpdateResult.Ok;
            }

            var now = DateTime.UtcNow;
            SensorUpdateResult result = SensorUpdateResult.Ok;

            var status = _sensorStatuses.AddOrUpdate(sensorId,
                // Add new
                (key) => new SensorStatus {
                    SensorId = key,
                    LastHeartbeat = now,
                    LastKnownWeight = weight
                },
                // Update existing
                (key, existingVal) => {
                    existingVal.LastHeartbeat = now;
                    existingVal.LastKnownWeight = weight;
                    return existingVal;
                });

            // --- Restocking Timer Logic ---
            if (weight < RESTOCKING_WEIGHT_THRESHOLD && !status.IsRestockingTimerActive)
            {
                // Start timer if weight drops below threshold
                status.IsRestockingTimerActive = true;
                status.RestockingTimerStartedUtc = now;
                result = SensorUpdateResult.RestockingTimerStarted;
                Console.WriteLine($"Restocking timer STARTED for Sensor ID: {sensorId} at {now}. Weight: {weight}");
            }
            else if (weight >= RESTOCKING_WEIGHT_THRESHOLD && status.IsRestockingTimerActive)
            {
                // Cancel timer if weight goes back up
                status.IsRestockingTimerActive = false;
                status.RestockingTimerStartedUtc = null;
                result = SensorUpdateResult.RestockingTimerCancelled;
                 Console.WriteLine($"Restocking timer CANCELLED for Sensor ID: {sensorId} at {now}. Weight: {weight}");
            }

            // --- Missing Product Check (only if timer is active) ---
            if (status.IsRestockingTimerActive && status.RestockingTimerStartedUtc.HasValue)
            {
                if ((now - status.RestockingTimerStartedUtc.Value) > MISSING_PRODUCT_DELAY)
                {
                    Console.WriteLine($"Missing product timer EXPIRED for Sensor ID: {sensorId}. Started: {status.RestockingTimerStartedUtc.Value}, Now: {now}");
                    // Timer expired, trigger alert and reset
                    result = SensorUpdateResult.MissingProductAlertNeeded;
                    status.IsRestockingTimerActive = false; // Reset timer state
                    status.RestockingTimerStartedUtc = null;
                }
            }

            // Re-assign status to ensure timer changes are reflected in the dictionary entry
            // (Handles potential concurrency nuances with AddOrUpdate's updateValueFactory)
             _sensorStatuses[sensorId] = status;

            return result;
        }
        
        public SensorStatus? GetSensorStatus(string sensorId)
        {
             _sensorStatuses.TryGetValue(sensorId, out var status);
             return status;
        }

        public List<SensorStatus> GetActiveSensors(TimeSpan timeout)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeout);

            return _sensorStatuses.Values
                .Where(s => s.LastHeartbeat >= cutoffTime)
                .OrderBy(s => s.SensorId)
                .ToList();
        }
    }
}
