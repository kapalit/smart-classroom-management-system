//TelemetryPoint model for telemetry publishing
namespace SmartClassroom.Core.Models
{
    public class TelemetryPoint
    {
        public string DeviceId { get; set; }
        public string Metric { get; set; }
        public object Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string Unit { get; set; }

        public TelemetryPoint()
        {
            Timestamp = DateTime.Now;
        }
    }
}