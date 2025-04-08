using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Stock_Ease.Services
{
    public class SensorStatus
    {
        public string SensorId { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; }
    }

    public interface IWeightSensorStatusService
    {
        void RecordHeartbeat(string sensorId);
        List<SensorStatus> GetActiveSensors(TimeSpan timeout);
    }

    public class WeightSensorStatusService : IWeightSensorStatusService
    {
        // Use ConcurrentDictionary for thread-safe access from multiple requests
        private readonly ConcurrentDictionary<string, SensorStatus> _sensorStatuses = new ConcurrentDictionary<string, SensorStatus>();

        public void RecordHeartbeat(string sensorId)
        {
            if (string.IsNullOrWhiteSpace(sensorId)) return;

            var status = new SensorStatus
            {
                SensorId = sensorId,
                LastHeartbeat = DateTime.UtcNow // Use UTC for consistency
            };

            // Add or update the sensor status
            _sensorStatuses.AddOrUpdate(sensorId, status, (key, existingVal) => {
                existingVal.LastHeartbeat = status.LastHeartbeat;
                return existingVal;
            });

            // Optional: Clean up very old entries periodically if needed,
            // but for now, we'll just rely on the timeout check in GetActiveSensors.
        }

        public List<SensorStatus> GetActiveSensors(TimeSpan timeout)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeout);

            // Filter sensors whose last heartbeat is within the timeout period
            return _sensorStatuses.Values
                .Where(s => s.LastHeartbeat >= cutoffTime)
                .OrderBy(s => s.SensorId) // Consistent ordering
                .ToList();
        }
    }
}
