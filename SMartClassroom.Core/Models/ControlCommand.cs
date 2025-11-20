// ControlCommand model for HVAC and device control
namespace SmartClassroom.Core.Models
{
    public class ControlCommand
    {
        public string TargetId { get; set; }
        public string Command { get; set; }
        public Dictionary<string, object> Args { get; set; }
        public string CorrelationId { get; set; }

        public ControlCommand()
        {
            Args = new Dictionary<string, object>();
            CorrelationId = Guid.NewGuid().ToString();
        }
    }
}