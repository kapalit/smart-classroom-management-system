//ITelemetryBus interface for telemetry publishing
using SmartClassroom.Core.Models;

namespace SmartClassroom.Core.Interfaces
{
    public interface ITelemetryBus
    {
        void Publish(TelemetryPoint telemetry);
        void Publish(DeviceState state);
    }
}