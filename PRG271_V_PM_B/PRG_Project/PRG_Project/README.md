**1. System Overview**

This project simulates real time management for a space station composed of individual modules, 
namely Life Support and Power Grid. Each module handles its own internal mechanics.
While the user works through the console menu, a background thread continuously monitors
every active module. If a system drops below safe limits, the monitor triggers real time alerts
and maintenance flags automatically. The goal was to build a console app that feels like a live control room
rather than a static prompt and response tool.

**2. Quick Start**

1. Open PRG_Project.csproj in Visual Studio.
2. Press Ctrl + Shift + B to build the solution.
3. Run the application.
4. Once started, the system seeds two default modules and boots the background monitor thread.
5. Navigate the station using the numbered console options.
6. Enter 0 to safely exit

**3. Architecture & OOP Design**

Module Base Class: An abstract class holding common properties like integrity, 
power draw, and operational status. Property setters handle clamping logic directly 
(e.g. integrity stays between 0 and 100). 
Concrete classes (LifeSupportModule, PowerGridModule) override ProcessSimulationTick() and GetTelemetryReport().

Interface Contracts (ILog & IMaintenance): Both module types implement these interfaces. 
This allows the logger and repair routines to operate on any module generically without 
caring about its concrete implementation.

SimulationEngine: The core manager for the station. It maintains the module list, controls the background simulation, 
and exposes public methods (AssignCrew, TransferPower, PerformRepair) so the UI layer never mutates module state directly.

Decoupled Events (StationMonitor): The monitor evaluates module conditions and fires AlertRaised or 
MaintenanceRequired events. It has no direct references to logger or auto-repair classes—it simply emits events 
that external handlers listen for.

Domain-Specific Exceptions: Logic failures (like over-allocating crew or drawing power beyond capacity) 
throw custom exceptions (ModuleOperationException, ResourceLimitExceededException, CriticalSystemStateException). 
The menu loop catches these gracefully so bad input doesn't crash the console app.

**4. Multithreading & Synchronisation**

The engine spins up a dedicated background thread (MonitorLoop) via Start():
1. Locks the shared module collection and grabs a quick snapshot.
2. Calls ProcessSimulationTick() on each active module.
3. Runs checks through StationMonitor to raise alerts if metrics cross safety thresholds.
4. Pauses for 1000ms (Thread.Sleep) before repeating the loop.

To prevent race conditions between menu actions on the main thread and background updates, all list operations use a 
standard C# lock object. System shutdown uses a CancellationTokenSource paired with Join() to give the background thread 
time to finish its current loop before closing.

**5. Event Driven System**

The reactive engine uses two primary custom events:
1. AlertRaised (EventHandler<ModuleAlertEventArgs>): Triggers when oxygen, integrity, 
or power reserves drop into warning or critical zones.
2. MaintenanceRequired (EventHandler<MaintenanceRequiredEventArgs>): Triggers when a module's RequiresMaintenance flag trips.

**6. Telemetry & File Logging**

The StationLogger handles file output by subscribing to engine events and buffering log lines in memory. 
It uses the ILog interface to pull formatted telemetry snapshots from any module type. 
Logs are written to station_log.txt on command (Menu Option 11) or during the graceful shutdown sequence.