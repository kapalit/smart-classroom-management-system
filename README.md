# Smart Classroom Management System

A WPF-based smart classroom monitoring and management system built with C# and .NET.

## Features

- Real-time environmental monitoring (temperature, humidity, air quality)
- Alarm system for threshold violations
- Telemetry data collection and processing
- Modern WPF user interface with MVVM pattern
- Modular architecture for easy extension

## Project Structure

```
├── SmartClassroom/              # Main WPF application
│   ├── GUI-Views/              # User interface views
│   ├── GUI-ViewModels/         # MVVM view models
│   ├── Modules/                # Core functionality modules
│   ├── Shared/                 # Shared utilities and services
│   └── Data/                   # Data access layer
├── SmartClassroom.Core/        # Core business logic
├── SmartClassroom.Demo/        # Demo application
├── SmartClassroom.Tests/       # Unit tests
└── SmartClassroom.EnvironmentModule/ # Environment monitoring module
```

## Getting Started

### Prerequisites
- .NET 6.0 or later
- Visual Studio 2022 or Visual Studio Code
- Windows 10/11 (for WPF)

### Building
```bash
dotnet build SmartClassroom.slnx
```

### Running
```bash
dotnet run --project SmartClassroom
```

## Architecture

The system follows a modular architecture with:
- **TelemetryBus**: Central communication hub for sensor data
- **AlarmEngine**: Monitoring and alerting system
- **EnvironmentModule**: Environmental data processing
- **MVVM Pattern**: Clean separation of UI and business logic

## Course Information

**Course**: CSCN72020 - Section 1  
**Group**: Group 20  
**Project Type**: Smart Classroom Management System
