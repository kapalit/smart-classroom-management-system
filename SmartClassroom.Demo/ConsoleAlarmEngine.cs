using System;
using System.Collections.Generic;
using System.Linq;
using SmartClassroom.Core.Interfaces;
using SmartClassroom.Core.Models;

namespace SmartClassroom.Demo
{
    /// <summary>
    ///Console implementation of IAlarmEngine for demo purposes
    /// </summary>
    public class ConsoleAlarmEngine : IAlarmEngine
    {
        private List<AlarmEvent> _activeAlarms = new List<AlarmEvent>();

        public void RaiseAlarm(AlarmEvent alarm)
        {
            _activeAlarms.Add(alarm);

            // Color code by severity
            ConsoleColor color = alarm.Severity switch
            {
                "Critical" => ConsoleColor.Red,
                "Warning" => ConsoleColor.Yellow,
                "Info" => ConsoleColor.White,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"\n⚠ [ALARM] {alarm.Severity.ToUpper()} ⚠");
            Console.WriteLine($"   Device: {alarm.DeviceId}");
            Console.WriteLine($"   Message: {alarm.Message}");
            Console.WriteLine($"   Time: {alarm.StartTime:HH:mm:ss}");
            Console.ResetColor();
            Console.WriteLine();
        }

        public void ClearAlarm(string alarmId)
        {
            var alarm = _activeAlarms.FirstOrDefault(a => a.Id == alarmId);
            if (alarm != null)
            {
                alarm.EndTime = DateTime.Now;
                Console.WriteLine($"[ALARM CLEARED] {alarm.Id}");
            }
        }
    }
}