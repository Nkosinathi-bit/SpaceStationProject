using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PRG_Project
{
    /// <summary>
    /// engine responsible for background simulation,
    /// module monitoring, event routing, and domain constraints.
    internal class SimulationEngine
    {
        private readonly List<Module> _modules = new List<Module>();
        private readonly object _syncRoot = new object();
        private readonly StationMonitor _monitor = new StationMonitor();

        private Thread _monitorThread;
        private CancellationTokenSource _cts;
        private bool _isRunning;

        /// High-level events exposed to Person 3's UI/menu.
        internal event EventHandler<ModuleAlertEventArgs> AlertRaised;
        internal event EventHandler<MaintenanceRequiredEventArgs> MaintenanceRequired;

        internal SimulationEngine()
        {
            _monitor.AlertRaised += (sender, args) => AlertRaised?.Invoke(sender, args);
            _monitor.MaintenanceRequired += (sender, args) => MaintenanceRequired?.Invoke(sender, args);
        }


        /// Thread-safe snapshot of all registered modules.

        internal IReadOnlyList<Module> Modules
        {
            get
            {
                lock (_syncRoot)
                {
                    return _modules.ToList();
                }
            }
        }

        internal bool IsRunning => _isRunning;

        internal void AddModule(Module module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            lock (_syncRoot)
            {
                if (_modules.Any(m => m.id == module.id))
                {
                    throw new ModuleOperationException(
                        module.id,
                        module.name,
                        "A module with this ID is already registered.");
                }

                _modules.Add(module);
            }
        }

        internal void RemoveModule(string moduleId)
        {
            lock (_syncRoot)
            {
                Module module = _modules.FirstOrDefault(m => m.id == moduleId);

                if (module == null)
                {
                    throw new ModuleOperationException(moduleId, "Unknown", "Module not found.");
                }

                _modules.Remove(module);
                Console.WriteLine($"[ENGINE] Module {module.name} ({module.id}) removed from simulation.");
            }
        }

        internal Module FindModule(string moduleId)
        {
            lock (_syncRoot)
            {
                return _modules.FirstOrDefault(m => m.id == moduleId);
            }
        }

        internal void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _cts = new CancellationTokenSource();

            _monitorThread = new Thread(MonitorLoop)
            {
                IsBackground = true,
                Name = "StationMonitorThread"
            };

            _isRunning = true;
            _monitorThread.Start();

            Console.WriteLine("[ENGINE] Background station monitor started.");
        }

        internal void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _cts.Cancel();

            try
            {
                if (!_monitorThread.Join(2500))
                {
                    _monitorThread.Interrupt();
                }
            }
            catch (ThreadInterruptedException)
            {
                // Expected during shutdown.
            }

            _isRunning = false;
            Console.WriteLine("[ENGINE] Background station monitor stopped.");
        }

        internal void RegisterSubscriber(StationEventSubscriber subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            _monitor.AlertRaised += subscriber.OnAlertRaised;
            _monitor.MaintenanceRequired += subscriber.OnMaintenanceRequired;
        }

        internal void UnregisterSubscriber(StationEventSubscriber subscriber)
        {
            if (subscriber == null)
            {
                return;
            }

            _monitor.AlertRaised -= subscriber.OnAlertRaised;
            _monitor.MaintenanceRequired -= subscriber.OnMaintenanceRequired;
        }


        /// Allows Person 3 / testing to run one simulation tick manually.

        internal void RunManualTick()
        {
            Tick();
        }

        private void MonitorLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    Tick();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONITOR LOOP ERROR] {ex.Message}");
                }

                // Sleep for 1 second, but allow thread interruption to stop the loop.
                try
                {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }

        private void Tick()
        {
            List<Module> snapshot;

            lock (_syncRoot)
            {
                snapshot = _modules.ToList();
            }

            foreach (Module module in snapshot)
            {
                try
                {
                    module.ProcessSimulationTick();
                }
                catch (Exception ex)
                {
                    _monitor.RaiseAlert(
                        module,
                        "TICK_ERROR",
                        $"Processing failed: {ex.Message}");
                }
                finally
                {
                    NormalizeModule(module);
                    _monitor.EvaluateModule(module);
                }
            }

            EnforceDomainConstraints(snapshot);
        }

        /// <summary>
        /// Ensures Person 1's property setters/clamps are applied after their simulation logic.
        /// This fixes possible bypasses inside their ProcessSimulationTick methods.
        /// </summary>
        private void NormalizeModule(Module module)
        {
            if (module is LifeSupportModule lifeSupport)
            {
                lifeSupport.OxygenLevel = lifeSupport.OxygenLevel;
                lifeSupport.CurrentCrewCount = lifeSupport.CurrentCrewCount;
            }
            else if (module is PowerGridModule powerGrid)
            {
                powerGrid.EnergyGeneration = powerGrid.EnergyGeneration;
                powerGrid.StoredEnergy = powerGrid.StoredEnergy;
            }
            else if (module.Integrity > 100)
            {
                module.Integrity = 100;
            }
        }

        private void EnforceDomainConstraints(IEnumerable<Module> modules)
        {
            foreach (Module module in modules)
            {
                if (module is LifeSupportModule lifeSupport)
                {
                    if (lifeSupport.OxygenLevel < 10.0)
                    {
                        throw new CriticalSystemStateException(
                            module.id,
                            module.name,
                            $"Oxygen level critically low: {lifeSupport.OxygenLevel:F1}%");
                    }

                    if (lifeSupport.CurrentCrewCount > lifeSupport.maxCrewCount)
                    {
                        throw new ResourceLimitExceededException(
                            module.id,
                            module.name,
                            $"Crew capacity exceeded: {lifeSupport.CurrentCrewCount}/{lifeSupport.maxCrewCount}");
                    }
                }
                else if (module is PowerGridModule powerGrid)
                {
                    if (powerGrid.StoredEnergy <= 0)
                    {
                        throw new CriticalSystemStateException(
                            module.id,
                            module.name,
                            "Power grid reserves are depleted.");
                    }
                }
            }
        }

        // ---------------------------------------------------------------------
        // Domain operations called later by Person 3's menu.
        // These throw logical domain exceptions and demonstrate custom errors.
        // ---------------------------------------------------------------------

        internal void AssignCrew(string moduleId, int crewCount)
        {
            lock (_syncRoot)
            {
                Module module = FindModuleUnsafe(moduleId)
                    ?? throw new ModuleOperationException(
                        moduleId,
                        "Unknown",
                        "Module not found.");

                if (!(module is LifeSupportModule lifeSupport))
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Crew can only be assigned to a LifeSupportModule.");
                }

                if (lifeSupport.Status == ModuleStatus.Offline)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Cannot assign crew to an offline life support module.");
                }

                if (crewCount < 0)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Crew count cannot be negative.");
                }

                int remaining = lifeSupport.maxCrewCount - lifeSupport.CurrentCrewCount;

                if (crewCount > remaining)
                {
                    throw new ResourceLimitExceededException(
                        moduleId,
                        module.name,
                        $"Requested {crewCount} crew, but only {remaining} slots remain.");
                }

                lifeSupport.CurrentCrewCount += crewCount;

                Console.WriteLine(
                    $"[OPERATION] {crewCount} crew assigned to {module.name}. " +
                    $"Total: {lifeSupport.CurrentCrewCount}/{lifeSupport.maxCrewCount}");
            }
        }

        internal void RemoveCrew(string moduleId, int crewCount)
        {
            lock (_syncRoot)
            {
                Module module = FindModuleUnsafe(moduleId)
                    ?? throw new ModuleOperationException(
                        moduleId,
                        "Unknown",
                        "Module not found.");

                if (!(module is LifeSupportModule lifeSupport))
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Crew can only be removed from a LifeSupportModule.");
                }

                if (crewCount < 0)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Crew count cannot be negative.");
                }

                if (crewCount > lifeSupport.CurrentCrewCount)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Cannot remove more crew than currently assigned.");
                }

                lifeSupport.CurrentCrewCount -= crewCount;

                Console.WriteLine(
                    $"[OPERATION] {crewCount} crew removed from {module.name}. " +
                    $"Total: {lifeSupport.CurrentCrewCount}/{lifeSupport.maxCrewCount}");
            }
        }

        internal void TransferPower(string moduleId, double amount)
        {
            lock (_syncRoot)
            {
                Module module = FindModuleUnsafe(moduleId)
                    ?? throw new ModuleOperationException(
                        moduleId,
                        "Unknown",
                        "Module not found.");

                if (!(module is PowerGridModule powerGrid))
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Power transfer can only target a PowerGridModule.");
                }

                if (powerGrid.Status == ModuleStatus.Offline)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Cannot transfer power from an offline power grid.");
                }

                if (amount < 0)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Transfer amount cannot be negative.");
                }

                if (amount > powerGrid.StoredEnergy)
                {
                    throw new ResourceLimitExceededException(
                        moduleId,
                        module.name,
                        $"Cannot transfer {amount} kWh; only {powerGrid.StoredEnergy:F1} kWh stored.");
                }

                powerGrid.StoredEnergy -= amount;

                Console.WriteLine(
                    $"[OPERATION] Transferred {amount} kWh from {module.name}. " +
                    $"Remaining: {powerGrid.StoredEnergy:F1} kWh.");
            }
        }

        internal void PerformRepair(string moduleId, int repairValue)
        {
            lock (_syncRoot)
            {
                Module module = FindModuleUnsafe(moduleId)
                    ?? throw new ModuleOperationException(
                        moduleId,
                        "Unknown",
                        "Module not found.");

                if (!(module is IMaintenance repairable))
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Module is not repairable.");
                }

                if (repairValue <= 0)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Repair value must be positive.");
                }

                repairable.Repair(repairValue);

                if (module.Status == ModuleStatus.Offline && module.Integrity > 0)
                {
                    module.Status = ModuleStatus.Online;
                    Console.WriteLine($"[OPERATION] {module.name} restored to Online.");
                }

                Console.WriteLine(
                    $"[OPERATION] Repair performed on {module.name} (+{repairValue} integrity).");
            }
        }

        internal void EmergencyShutdown(string moduleId)
        {
            lock (_syncRoot)
            {
                Module module = FindModuleUnsafe(moduleId)
                    ?? throw new ModuleOperationException(
                        moduleId,
                        "Unknown",
                        "Module not found.");

                if (module.Status == ModuleStatus.Offline)
                {
                    throw new ModuleOperationException(
                        moduleId,
                        module.name,
                        "Module is already offline.");
                }

                module.Status = ModuleStatus.Offline;

                Console.WriteLine(
                    $"[OPERATION] Emergency shutdown executed on {module.name}.");
            }
        }

        private Module FindModuleUnsafe(string moduleId)
        {
            return _modules.FirstOrDefault(m => m.id == moduleId);
        }
    }
}
