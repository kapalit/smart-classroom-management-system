//DeviceState model representing device operational state
namespace SmartClassroom.Core.Models
{
    public class DeviceState
    {
        public string DeviceId { get; set; }
        public string Status { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public DeviceState()
        {
            Properties = new Dictionary<string, object>();
        }
    }
}
