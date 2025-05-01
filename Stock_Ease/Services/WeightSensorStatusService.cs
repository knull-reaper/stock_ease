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
        public string SensorId
        {
            get;
            set;
        } = string.Empty;
        public DateTime LastHeartbeat
        {
            get;
            set;
        }
        public double? LastKnownWeight
        {
            get;
            set;
        }
        public bool IsRestockingTimerActive
        {
            get;
            set;
        } = false;
        public DateTime? RestockingTimerStartedUtc
        {
            get;
            set;
        }
    }

    public interface IWeightSensorStatusService
    {
        SensorUpdateResult RecordSensorUpdate(string sensorId, double weight);
        List<SensorStatus> GetActiveSensors(TimeSpan timeout);
        SensorStatus? GetSensorStatus(string sensorId);
    }

    public class WeightSensorStatusService : IWeightSensorStatusService
    {
        private readonly ConcurrentDictionary<string, SensorStatus> _sensorStatuses = new ConcurrentDictionary<string, SensorStatus>();
        private
        const double RESTOCKING_WEIGHT_THRESHOLD = 3.0;
        private static readonly TimeSpan MISSING_PRODUCT_DELAY = TimeSpan.FromMinutes(10);

        public SensorUpdateResult RecordSensorUpdate(string sensorId, double weight)
        {
            if (string.IsNullOrWhiteSpace(sensorId))
            {

                return SensorUpdateResult.Ok;
            }

            var now = DateTime.UtcNow;
            SensorUpdateResult result = SensorUpdateResult.Ok;

            var status = _sensorStatuses.AddOrUpdate(sensorId,

              (key) => new SensorStatus
              {
                  SensorId = key,
                  LastHeartbeat = now,
                  LastKnownWeight = weight
              },

              (key, existingVal) =>
              {
                  existingVal.LastHeartbeat = now;
                  existingVal.LastKnownWeight = weight;
                  return existingVal;
              });

            if (weight < RESTOCKING_WEIGHT_THRESHOLD && !status.IsRestockingTimerActive)
            {

                status.IsRestockingTimerActive = true;
                status.RestockingTimerStartedUtc = now;
                result = SensorUpdateResult.RestockingTimerStarted;
                Console.WriteLine($"Restocking timer STARTED for Sensor ID: {sensorId} at {now}. Weight: {weight}");
            }
            else if (weight >= RESTOCKING_WEIGHT_THRESHOLD && status.IsRestockingTimerActive)
            {

                status.IsRestockingTimerActive = false;
                status.RestockingTimerStartedUtc = null;
                result = SensorUpdateResult.RestockingTimerCancelled;
                Console.WriteLine($"Restocking timer CANCELLED for Sensor ID: {sensorId} at {now}. Weight: {weight}");
            }

            if (status.IsRestockingTimerActive && status.RestockingTimerStartedUtc.HasValue)
            {
                if ((now - status.RestockingTimerStartedUtc.Value) > MISSING_PRODUCT_DELAY)
                {
                    Console.WriteLine($"Missing product timer EXPIRED for Sensor ID: {sensorId}. Started: {status.RestockingTimerStartedUtc.Value}, Now: {now}");

                    result = SensorUpdateResult.MissingProductAlertNeeded;
                    status.IsRestockingTimerActive = false;
                    status.RestockingTimerStartedUtc = null;
                }
            }

            _sensorStatuses[sensorId] = status;

            return result;
        }

        public SensorStatus? GetSensorStatus(string sensorId)
        {
            _sensorStatuses.TryGetValue(sensorId, out
              var status);
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