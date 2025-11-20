using System;
using System.Collections.Generic;
using SmartClassroom.Core.Interfaces;
using SmartClassroom.Core.Models;

namespace SmartClassroom.Demo
{
    /// <summary>
    ///Console implementation of ITelemetryBus for demo purposes
    /// </summary>
    public class ConsoleTelemetryBus : ITelemetryBus
    {
        private int _messageCount = 0;

        public void Publish(TelemetryPoint telemetry)
        {
            _messageCount++;

            // Color code by metric type
            ConsoleColor color = telemetry.Metric switch
            {
                "Temperature" => ConsoleColor.Cyan,
                "Humidity" => ConsoleColor.Blue,
                "CO2" => ConsoleColor.Magenta,
                "ComfortIndex" => ConsoleColor.Green,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"[TELEMETRY #{_messageCount:D3}] {telemetry.DeviceId,-25} | " +
                            $"{telemetry.Metric,-15}: {FormatValue(telemetry.Value)} {telemetry.Unit}");
            Console.ResetColor();
        }

        public void Publish(DeviceState state)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[STATE] {state.DeviceId,-25} | Status: {state.Status}");

            foreach (var prop in state.Properties)
            {
                Console.WriteLine($"        → {prop.Key}: {FormatValue(prop.Value)}");
            }
            Console.ResetColor();
        }

        private string FormatValue(object value)
        {
            if (value is double d)
                return d.ToString("F1");
            return value?.ToString() ?? "null";
        }
    }
}
