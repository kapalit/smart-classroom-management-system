using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartClassroom.Core.Interfaces;
using SmartClassroom.Core.Models;

namespace SmartClassroom.Modules
{
    /// <summary>
    /// Reads sensor data from a CSV file in a looping manner.
    /// Single Responsibility: Only handles CSV data reading.
    /// </summary>
    public class CsvDataReader : IDataReader
    {
        private List<SensorReading> _readings;
        private int _currentIndex;
        private readonly object _lock = new object();

        public bool IsInitialized { get; private set; }

        /// <summary>
        ///Initialize the reader with a CSV file
        /// </summary>
        public void Initialize(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Data file not found: {filePath}");

            _readings = new List<SensorReading>();
            _currentIndex = 0;

            try
            {
                ParseCsvFile(filePath);
                IsInitialized = true;
                Console.WriteLine($"[CsvDataReader] Loaded {_readings.Count} readings from {filePath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse CSV file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse the CSV file and load all readings
        /// </summary>
        private void ParseCsvFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
                throw new InvalidOperationException("CSV file must have at least a header and one data row");

            // Skip header (line 0)
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var reading = ParseCsvLine(line);
                    _readings.Add(reading);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CsvDataReader] Warning: Skipping invalid line {i}: {ex.Message}");
                    // Continue parsing other lines
                }
            }

            if (_readings.Count == 0)
                throw new InvalidOperationException("No valid readings found in CSV file");
        }

        /// <summary>
        /// Parse a single CSV line into a SensorReading
        /// Format: Timestamp,RoomId,Temperature,Humidity,CO2
        /// </summary>
        private SensorReading ParseCsvLine(string line)
        {
            var parts = line.Split(',');

            if (parts.Length < 5)
                throw new FormatException("CSV line must have 5 fields: Timestamp,RoomId,Temperature,Humidity,CO2");

            return new SensorReading
            {
                Timestamp = DateTime.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                RoomId = parts[1].Trim(),
                Temperature = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                Humidity = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                CO2 = double.Parse(parts[4].Trim(), CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// Get the next reading (loops back to start when reaching end)
        /// </summary>
        public SensorReading GetNextReading()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Reader is not initialized. Call Initialize() first.");

            lock (_lock)
            {
                var reading = _readings[_currentIndex];

                _currentIndex++;
                if (_currentIndex >= _readings.Count)
                {
                    _currentIndex = 0; // Loop back to start
                }

                return reading;
            }
        }
    }
}


