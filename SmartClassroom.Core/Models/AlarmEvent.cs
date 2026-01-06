//AlarmEvent model for system warning and critical alerts
namespace SmartClassroom.Core.Models
{
    public class AlarmEvent
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public string Severity { get; set; } // Info, Warning, Critical
        public string Message { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public AlarmEvent()
        {
            Id = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
        }
    }
}
