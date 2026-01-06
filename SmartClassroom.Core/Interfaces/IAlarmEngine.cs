//IAlarmEngine interface for alarm handling
using SmartClassroom.Core.Models;

namespace SmartClassroom.Core.Interfaces
{
    public interface IAlarmEngine
    {
        void RaiseAlarm(AlarmEvent alarm);
        void ClearAlarm(string alarmId);
    }
}
