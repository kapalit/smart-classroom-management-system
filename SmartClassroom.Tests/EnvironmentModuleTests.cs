using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using SmartClassroom.Core.Interfaces;
using SmartClassroom.Core.Models;
using SmartClassroom.Core.Enums;
using SmartClassroom.Modules;

namespace SmartClassroom.Tests
{
    [TestFixture]
    public class EnvironmentModuleTests
    {
        private Mock<IDataReader> _mockDataReader;
        private Mock<ITelemetryBus> _mockTelemetryBus;
        private Mock<IAlarmEngine> _mockAlarmEngine;

        [SetUp]
        public void Setup()
        {
            _mockDataReader = new Mock<IDataReader>();
            _mockTelemetryBus = new Mock<ITelemetryBus>();
            _mockAlarmEngine = new Mock<IAlarmEngine>();

            // Setup data reader as initialized by default
            _mockDataReader.Setup(r => r.IsInitialized).Returns(true);
        }

        [Test]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            // Act
            var module = new EnvironmentModule(
                "TestModule",
                "Room101",
                _mockDataReader.Object,
                _mockTelemetryBus.Object,
                _mockAlarmEngine.Object
            );

            // Assert
            Assert.That(module.ModuleId, Is.EqualTo("TestModule"));
            Assert.That(module.RoomId, Is.EqualTo("Room101"));
        }

        [Test]
        public void Constructor_NullParameters_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EnvironmentModule(null, "Room101", 
                    _mockDataReader.Object, 
                    _mockTelemetryBus.Object, 
                    _mockAlarmEngine.Object));
        }

        [Test]
        public void Start_DataReaderNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockDataReader.Setup(r => r.IsInitialized).Returns(false);
            var module = CreateModule();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => module.Start());
        }

        [Test]
        public void Start_ValidSetup_PublishesTelemetry()
        {
            // Arrange
            var testReading = new SensorReading
            {
                Temperature = 22.0,
                Humidity = 50.0,
                CO2 = 600.0,
                RoomId = "Room101"
            };

            _mockDataReader.Setup(r => r.GetNextReading()).Returns(testReading);
            var module = CreateModule();

            // Act
            module.Start();
            Thread.Sleep(2500); // Wait for at least one cycle
            module.Stop();

            // Assert - Verify telemetry was published
            _mockTelemetryBus.Verify(b => b.Publish(
                It.Is<TelemetryPoint>(t => t.Metric == "Temperature")), 
                Times.AtLeastOnce);
            
            _mockTelemetryBus.Verify(b => b.Publish(
                It.Is<TelemetryPoint>(t => t.Metric == "Humidity")), 
                Times.AtLeastOnce);
            
            _mockTelemetryBus.Verify(b => b.Publish(
                It.Is<TelemetryPoint>(t => t.Metric == "CO2")), 
                Times.AtLeastOnce);
        }

        [Test]
        public void ProcessControlCommand_SetValidTemperature_UpdatesSetpoint()
        {
            // Arrange
            var module = CreateModule();
            var command = new ControlCommand
            {
                TargetId = "Room101_HVAC",
                Command = "SetTemperature",
                Args = new Dictionary<string, object> { { "setpoint", 23.0 } }
            };

            // Act
            module.ProcessControlCommand(command);

            // Assert
            Assert.That(module.GetHvacSetpoint(), Is.EqualTo(23.0));
            _mockTelemetryBus.Verify(b => b.Publish(
                It.Is<DeviceState>(d => d.DeviceId == "Room101_HVAC")), 
                Times.Once);
        }

        [Test]
        public void ProcessControlCommand_InvalidTemperature_RaisesAlarm()
        {
            // Arrange
            var module = CreateModule();
            var command = new ControlCommand
            {
                TargetId = "Room101_HVAC",
                Command = "SetTemperature",
                Args = new Dictionary<string, object> { { "setpoint", 35.0 } } // Too high
            };

            // Act
            module.ProcessControlCommand(command);

            // Assert
            _mockAlarmEngine.Verify(e => e.RaiseAlarm(
                It.Is<AlarmEvent>(a => a.Severity == "Warning")), 
                Times.Once);
        }

        [Test]
        public void ProcessControlCommand_SetFanSpeed_UpdatesFanSpeed()
        {
            // Arrange
            var module = CreateModule();
            var command = new ControlCommand
            {
                TargetId = "Room101_HVAC",
                Command = "SetFanSpeed",
                Args = new Dictionary<string, object> { { "speed", "High" } }
            };

            // Act
            module.ProcessControlCommand(command);

            // Assert
            _mockTelemetryBus.Verify(b => b.Publish(
                It.Is<DeviceState>(d => 
                    d.DeviceId == "Room101_HVAC" && 
                    d.Properties["FanSpeed"].ToString() == "High")), 
                Times.Once);
        }

        [Test]
        public void ProcessControlCommand_WrongTargetId_IgnoresCommand()
        {
            // Arrange
            var module = CreateModule();
            var command = new ControlCommand
            {
                TargetId = "Room999_HVAC", // Different room
                Command = "SetTemperature",
                Args = new Dictionary<string, object> { { "setpoint", 23.0 } }
            };

            double originalSetpoint = module.GetHvacSetpoint();

            // Act
            module.ProcessControlCommand(command);

            // Assert - Setpoint should not change
            Assert.That(module.GetHvacSetpoint(), Is.EqualTo(originalSetpoint));
        }

        [Test]
        public void ComfortIndex_OptimalConditions_ReturnsHighScore()
        {
            // Arrange
            var testReading = new SensorReading
            {
                Temperature = 22.0,  // Optimal
                Humidity = 45.0,     // Optimal
                CO2 = 500.0,         // Optimal
                RoomId = "Room101"
            };

            _mockDataReader.Setup(r => r.GetNextReading()).Returns(testReading);
            var module = CreateModule();

            // Act
            module.Start();
            Thread.Sleep(2500);
            module.Stop();

            double comfortIndex = module.GetComfortIndex();

            // Assert
            Assert.That(comfortIndex, Is.GreaterThan(90.0));
        }

        [Test]
        public void ComfortIndex_PoorConditions_ReturnsLowScore()
        {
            // Arrange
            var testReading = new SensorReading
            {
                Temperature = 30.0,  // Too hot
                Humidity = 80.0,     // Too humid
                CO2 = 1800.0,        // Too high
                RoomId = "Room101"
            };

            _mockDataReader.Setup(r => r.GetNextReading()).Returns(testReading);
            var module = CreateModule();

            // Act
            module.Start();
            Thread.Sleep(2500);
            module.Stop();

            double comfortIndex = module.GetComfortIndex();

            // Assert
            Assert.That(comfortIndex, Is.LessThan(50.0));
        }

        private EnvironmentModule CreateModule()
        {
            return new EnvironmentModule(
                "TestModule",
                "Room101",
                _mockDataReader.Object,
                _mockTelemetryBus.Object,
                _mockAlarmEngine.Object
            );
        }
    }

    [TestFixture]
    public class CsvDataReaderTests
    {
        private string _testCsvPath;

        [SetUp]
        public void Setup()
        {
            // Create a test CSV file
            _testCsvPath = Path.GetTempFileName();
            File.WriteAllText(_testCsvPath, @"Timestamp,RoomId,Temperature,Humidity,CO2
2025-10-29T09:00:00,Room101,22.5,45.0,450
2025-10-29T09:00:02,Room101,22.6,45.2,455
2025-10-29T09:00:04,Room101,22.7,45.1,460");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testCsvPath))
                File.Delete(_testCsvPath);
        }

        [Test]
        public void Initialize_ValidCsv_LoadsData()
        {
            // Arrange
            var reader = new CsvDataReader();

            // Act
            reader.Initialize(_testCsvPath);

            // Assert
            Assert.That(reader.IsInitialized, Is.True);
        }

        [Test]
        public void GetNextReading_LoopsData_ReturnsFirstAfterLast()
        {
            // Arrange
            var reader = new CsvDataReader();
            reader.Initialize(_testCsvPath);

            // Act - Read all 3 readings plus one more
            var reading1 = reader.GetNextReading();
            var reading2 = reader.GetNextReading();
            var reading3 = reader.GetNextReading();
            var reading4 = reader.GetNextReading(); // Should loop back to first

            // Assert
            Assert.That(reading1.Temperature, Is.EqualTo(22.5).Within(0.01));
            Assert.That(reading4.Temperature, Is.EqualTo(22.5).Within(0.01)); // Looped
        }

        [Test]
        public void Initialize_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            var reader = new CsvDataReader();

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => 
                reader.Initialize("nonexistent.csv"));
        }
    }

    [TestFixture]
    public class ComfortIndexCalculatorTests
    {
        private ComfortIndexCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _calculator = new ComfortIndexCalculator();
        }

        [Test]
        public void Calculate_OptimalConditions_Returns100()
        {
            // Act
            double score = _calculator.Calculate(22.0, 45.0, 500.0);

            // Assert
            Assert.That(score, Is.EqualTo(100.0).Within(0.1));
        }

        [Test]
        public void Calculate_HighTemperature_ReturnsLowerScore()
        {
            // Act
            double score = _calculator.Calculate(28.0, 45.0, 500.0);

            // Assert
            Assert.That(score, Is.LessThan(100.0));
        }

        [Test]
        public void Calculate_HighCO2_ReturnsLowerScore()
        {
            //Act
            double score = _calculator.Calculate(22.0, 45.0, 1600.0);

            //Assert
            Assert.That(score, Is.LessThan(80.0));
        }

        [Test]
        public void Calculate_AllPoorConditions_ReturnsVeryLowScore()
        {
            // Act
            double score = _calculator.Calculate(30.0, 80.0, 2000.0);

            //Assert
            Assert.That(score, Is.LessThan(30.0));
        }
    }
}
