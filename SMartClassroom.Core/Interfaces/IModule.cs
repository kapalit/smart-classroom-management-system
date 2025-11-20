//IModule interface to standardize module lifecycle
using SmartClassroom.Core.Models;

namespace SmartClassroom.Core.Interfaces
{
    public interface IModule
    {
        string ModuleId { get; }
        string RoomId { get; }
        void Start();
        void Stop();
        void ProcessControlCommand(ControlCommand command);
    }
}