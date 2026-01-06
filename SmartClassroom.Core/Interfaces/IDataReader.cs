//IDataReader interface for sensor data input
using SmartClassroom.Core.Models;

namespace SmartClassroom.Core.Interfaces
{
    public interface IDataReader
    {
        SensorReading GetNextReading();
        void Initialize(string filePath);
        bool IsInitialized { get; }
    }
}
