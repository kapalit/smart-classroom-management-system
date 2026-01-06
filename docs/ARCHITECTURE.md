# Architecture Overview

## System Components

### Core Components
- **TelemetryBus**: Central message bus for sensor data communication
- **AlarmEngine**: Threshold monitoring and alert generation
- **EnvironmentModule**: Environmental sensor data processing

### User Interface
- **MainWindow**: Primary application window
- **ViewModels**: MVVM pattern implementation for data binding
- **Views**: WPF user controls and windows

### Data Flow
1. Environmental sensors → EnvironmentModule
2. EnvironmentModule → TelemetryBus
3. TelemetryBus → AlarmEngine (threshold checking)
4. TelemetryBus → ViewModels (UI updates)

## Module Dependencies
```
SmartClassroom (Main App)
├── SmartClassroom.Core
├── SmartClassroom.EnvironmentModule
└── Shared Components
```
