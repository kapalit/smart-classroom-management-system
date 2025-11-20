using System;
using System.Collections.Generic;
using System.Threading;
using SmartClassroom.Core.Interfaces;
using SmartClassroom.Core.Models;
using SmartClassroom.Core.Enums;

namespace SmartClassroom.Modules
{
    /// <summary>
    /// Monitors environmental conditions (temperature, humidity, CO2) 
    /// and controls HVAC system for a classroom.
    /// Follows SOLID principles with dependency injection.
    /// </summary>
    public class EnvironmentModule : IModule
    {
        // Module identification
        public string ModuleId { get; }
        public string RoomId { get; }

        // Current sensor readings
        private double _temperature;
        private double _humidity;
        private double _co2Level;
        private double _comfortIndex;

        // HVAC control state
        private double _hvacSetpoint = 22.0; // Default 22°C
        private FanSpeed _fanSpeed = FanSpeed.Medium;

        // Dependencies (Dependency Injection - SOLID D)
        private readonly IDataReader _dataReader;
        private readonly ITelemetryBus _telemetryBus;
        private readonly IAlarmEngine _alarmEngine;
        private readonly ComfortIndexCalculator _comfortCalculator;

        // Threading for continuous operation
        private Thread _updateThread;
        private bool _isRunning;
        private readonly object _lock = new object();

        // Alarm debouncing
        private Dictionary<string, DateTime> _lastAlarmTime;
        private const int ALARM_DEBOUNCE_SECONDS = 30;

        //Configuration
        private const int UPDATE_INTERVAL_MS = 2000; // 2 seconds
        private const double MIN_SETPOINT = 16.0;
        private const double MAX_SETPOINT = 30.0;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public EnvironmentModule(
            string moduleId,
            string roomId,
            IDataReader dataReader,
            ITelemetryBus telemetryBus,
            IAlarmEngine alarmEngine)
        {
            ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            RoomId = roomId ?? throw new ArgumentNullException(nameof(roomId));
            _dataReader = dataReader ?? throw new ArgumentNullException(nameof(dataReader));
            _telemetryBus = telemetryBus ?? throw new ArgumentNullException(nameof(telemetryBus));
            _alarmEngine = alarmEngine ?? throw new ArgumentNullException(nameof(alarmEngine));

            _comfortCalculator = new ComfortIndexCalculator();
            _lastAlarmTime = new Dictionary<string, DateTime>();
        }

        /// <summary>
        /// Start the module - begins reading data and publishing telemetry
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            if (!_dataReader.IsInitialized)
                throw new InvalidOperationException("Data reader is not initialized");

            _isRunning = true;
            _updateThread = new Thread(UpdateLoop);
            _updateThread.IsBackground = true;
            _updateThread.Start();

            Console.WriteLine($"[{ModuleId}] Started for {RoomId}");
        }

        /// <summary>
        /// Stop the module
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _updateThread?.Join(5000); // Wait up to 5 seconds
            Console.WriteLine($"[{ModuleId}] Stopped");
        }

        /// <summary>
        /// Main update loop - runs in background thread
        /// </summary>
        private void UpdateLoop()
        {
            while (_isRunning)
            {
                try
                {
                    // Read latest sensor data
                    ReadSensorData();

                    // Calculate comfort index
                    CalculateComfortIndex();

                    // Check thresholds and raise alarms if needed
                    CheckThresholds();

                    // Publish telemetry to the bus
                    PublishTelemetry();

                    // Wait before next update
                    Thread.Sleep(UPDATE_INTERVAL_MS);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{ModuleId}] Error in update loop: {ex.Message}");
                    // Continue running even if one iteration fails
                }
            }
        }

        /// <summary>
        /// Read sensor data from the data reader
        /// </summary>
        private void ReadSensorData()
        {
            try
            {
                var reading = _dataReader.GetNextReading();

                lock (_lock)
                {
                    _temperature = reading.Temperature;
                    _humidity = reading.Humidity;
                    _co2Level = reading.CO2;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{ModuleId}] Error reading sensor data: {ex.Message}");
                // Keep previous values on error
            }
        }

        /// <summary>
        /// Calculate the comfort index based on current conditions
        /// </summary>
        private void CalculateComfortIndex()
        {
            lock (_lock)
            {
                _comfortIndex = _comfortCalculator.Calculate(
                    _temperature,
                    _humidity,
                    _co2Level
                );
            }
        }

        /// <summary>
        /// Check if any sensor readings exceed thresholds
        /// </summary>
        private void CheckThresholds()
        {
            lock (_lock)
            {
                // Temperature threshold: 18-26°C
                if (_temperature < 18 || _temperature > 26)
                {
                    RaiseAlarmIfNeeded(
                        "Temperature",
                        "Warning",
                        $"Temperature out of comfort range: {_temperature:F1}°C (normal: 18-26°C)"
                    );
                }

                // Humidity threshold: 30-60%
                if (_humidity < 30 || _humidity > 60)
                {
                    RaiseAlarmIfNeeded(
                        "Humidity",
                        "Warning",
                        $"Humidity out of comfort range: {_humidity:F1}% (normal: 30-60%)"
                    );
                }

                // CO₂ thresholds
                if (_co2Level > 1500)
                {
                    RaiseAlarmIfNeeded(
                        "CO2",
                        "Critical",
                        $"CO₂ level critical: {_co2Level:F0} ppm (limit: 1500 ppm) - Ventilation required!"
                    );
                }
                else if (_co2Level > 1000)
                {
                    RaiseAlarmIfNeeded(
                        "CO2",
                        "Warning",
                        $"CO₂ level elevated: {_co2Level:F0} ppm (limit: 1000 ppm)"
                    );
                }
            }
        }

        /// <summary>
        /// Raise an alarm with debouncing to avoid spam
        /// </summary>
        private void RaiseAlarmIfNeeded(string alarmKey, string severity, string message)
        {
            // Check if we've recently raised this alarm (debouncing)
            if (_lastAlarmTime.TryGetValue(alarmKey, out var lastTime))
            {
                if ((DateTime.Now - lastTime).TotalSeconds < ALARM_DEBOUNCE_SECONDS)
                    return; // Too soon, skip this alarm
            }

            _lastAlarmTime[alarmKey] = DateTime.Now;

            var alarm = new AlarmEvent
            {
                DeviceId = $"{RoomId}_{alarmKey}",
                Severity = severity,
                Message = message
            };

            _alarmEngine.RaiseAlarm(alarm);
        }

        /// <summary>
        /// Publish all telemetry points to the bus
        /// </summary>
        private void PublishTelemetry()
        {
            lock (_lock)
            {
                // Publish temperature
                _telemetryBus.Publish(new TelemetryPoint
                {
                    DeviceId = $"{RoomId}_TempSensor",
                    Metric = "Temperature",
                    Value = _temperature,
                    Unit = "°C"
                });

                // Publish humidity
                _telemetryBus.Publish(new TelemetryPoint
                {
                    DeviceId = $"{RoomId}_HumiditySensor",
                    Metric = "Humidity",
                    Value = _humidity,
                    Unit = "%"
                });

                // Publish CO₂
                _telemetryBus.Publish(new TelemetryPoint
                {
                    DeviceId = $"{RoomId}_CO2Sensor",
                    Metric = "CO2",
                    Value = _co2Level,
                    Unit = "ppm"
                });

                // Publish comfort index
                _telemetryBus.Publish(new TelemetryPoint
                {
                    DeviceId = $"{RoomId}_ComfortIndex",
                    Metric = "ComfortIndex",
                    Value = _comfortIndex,
                    Unit = "score"
                });

                // Publish HVAC state
                PublishHvacState();
            }
        }

        /// <summary>
        /// Publish HVAC device state
        /// </summary>
        private void PublishHvacState()
        {
            _telemetryBus.Publish(new DeviceState
            {
                DeviceId = $"{RoomId}_HVAC",
                Status = "Active",
                Properties = new Dictionary<string, object>
                {
                    { "Setpoint", _hvacSetpoint },
                    { "FanSpeed", _fanSpeed.ToString() },
                    { "CurrentTemp", _temperature },
                    { "Mode", DetermineHvacMode() }
                }
            });
        }

        /// <summary>
        /// Determine HVAC mode based on setpoint vs current temp
        /// </summary>
        private string DetermineHvacMode()
        {
            double diff = _temperature - _hvacSetpoint;

            if (Math.Abs(diff) < 0.5)
                return "Idle";
            else if (diff > 0.5)
                return "Cooling";
            else
                return "Heating";
        }

        /// <summary>
        /// Process a control command from the GUI or bus
        /// </summary>
        public void ProcessControlCommand(ControlCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Check if this command is for our HVAC system
            if (command.TargetId != $"{RoomId}_HVAC")
                return; // Not for us

            try
            {
                switch (command.Command)
                {
                    case "SetTemperature":
                        SetTemperatureSetpoint(command);
                        break;

                    case "SetFanSpeed":
                        SetFanSpeed(command);
                        break;

                    default:
                        Console.WriteLine($"[{ModuleId}] Unknown command: {command.Command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{ModuleId}] Error processing command: {ex.Message}");

                _alarmEngine.RaiseAlarm(new AlarmEvent
                {
                    DeviceId = $"{RoomId}_HVAC",
                    Severity = "Warning",
                    Message = $"Failed to process command '{command.Command}': {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Set the temperature setpoint for HVAC
        /// </summary>
        private void SetTemperatureSetpoint(ControlCommand command)
        {
            if (!command.Args.ContainsKey("setpoint"))
            {
                throw new ArgumentException("Missing 'setpoint' argument");
            }

            double setpoint = Convert.ToDouble(command.Args["setpoint"]);

            // Validate range
            if (setpoint < MIN_SETPOINT || setpoint > MAX_SETPOINT)
            {
                _alarmEngine.RaiseAlarm(new AlarmEvent
                {
                    DeviceId = $"{RoomId}_HVAC",
                    Severity = "Warning",
                    Message = $"Invalid setpoint: {setpoint}°C (valid range: {MIN_SETPOINT}-{MAX_SETPOINT}°C)"
                });
                return;
            }

            lock (_lock)
            {
                _hvacSetpoint = setpoint;
            }

            Console.WriteLine($"[{ModuleId}] Setpoint updated to {setpoint}°C");
            PublishHvacState();
        }

        /// <summary>
        /// Set the fan speed for HVAC
        /// </summary>
        private void SetFanSpeed(ControlCommand command)
        {
            if (!command.Args.ContainsKey("speed"))
            {
                throw new ArgumentException("Missing 'speed' argument");
            }

            string speedStr = command.Args["speed"].ToString();

            if (!Enum.TryParse<FanSpeed>(speedStr, true, out var fanSpeed))
            {
                throw new ArgumentException($"Invalid fan speed: {speedStr}");
            }

            lock (_lock)
            {
                _fanSpeed = fanSpeed;
            }

            Console.WriteLine($"[{ModuleId}] Fan speed set to {fanSpeed}");
            PublishHvacState();
        }

        /// <summary>
        /// Get current temperature (for testing)
        /// </summary>
        public double GetTemperature()
        {
            lock (_lock)
            {
                return _temperature;
            }
        }

        /// <summary>
        /// Get current comfort index (for testing)
        /// </summary>
        public double GetComfortIndex()
        {
            lock (_lock)
            {
                return _comfortIndex;
            }
        }

        /// <summary>
        /// Get current HVAC setpoint (for testing)
        /// </summary>
        public double GetHvacSetpoint()
        {
            lock (_lock)
            {
                return _hvacSetpoint;
            }
        }
    }
}