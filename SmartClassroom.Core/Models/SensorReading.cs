//SensorReading model for environmental data
namespace SmartClassroom.Core.Models
{
    public class SensorReading
    {
        public DateTime Timestamp { get; set; }
        public string RoomId { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double CO2 { get; set; }

        public SensorReading()
        {
            Timestamp = DateTime.Now;
        }
    }
}