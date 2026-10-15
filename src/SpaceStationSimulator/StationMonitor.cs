using System;

namespace PRG_Project
{
    /// Background monitor publisher that evaluates modules and raises events.
    internal class StationMonitor
    {
        public event EventHandler<ModuleAlertEventArgs> AlertRaised;
        public event EventHandler<MaintenanceRequiredEventArgs> MaintenanceRequired;

        /// Evaluates a module after every simulation tick and raises relevant events.
        public void EvaluateModule(Module module)
        {
            if (module == null)
            {
                return;
            }

            // Integrity checks
            if (module.Integrity <= 0)
            {
                RaiseAlert(module, "CRITICAL_FAILURE",
                    $"{module.name} integrity reached zero and is offline.");
            }
            else if (module.Integrity < 30)
            {
                RaiseAlert(module, "DAMAGE_WARNING",
                    $"{module.name} integrity is {module.Integrity}%.");
            }

            // Maintenance contract
            if (module is IMaintenance maintainable && maintainable.requiresMaintenance)
            {
                RaiseMaintenanceRequired(module, "Maintenance threshold reached.");
            }

            // Life support domain constraints
            if (module is LifeSupportModule lifeSupport)
            {
                if (lifeSupport.OxygenLevel < 10.0)
                {
                    RaiseAlert(module, "OXYGEN_CRITICAL",
                        $"Oxygen level is {lifeSupport.OxygenLevel:F1}%.");
                }
                else if (lifeSupport.OxygenLevel < 30.0)
                {
                    RaiseAlert(module, "OXYGEN_WARNING",
                        $"Oxygen level is {lifeSupport.OxygenLevel:F1}%.");
                }
            }

            // Power grid domain constraints
            if (module is PowerGridModule powerGrid)
            {
                if (!powerGrid.isGridStable)
                {
                    RaiseAlert(module, "GRID_UNSTABLE",
                        "Power grid is draining faster than it generates.");
                }

                if (powerGrid.StoredEnergy < powerGrid.storageCapacity * 0.20)
                {
                    RaiseAlert(module, "RESERVE_LOW",
                        $"Power reserves are at {powerGrid.StoredEnergy:F1} kWh.");
                }
            }
        }

        /// Manually raises an alert event, safely invoking all subscribers.
        public void RaiseAlert(Module module, string alertType, string message)
        {
            EventHandler<ModuleAlertEventArgs> handler = AlertRaised;

            if (handler == null)
            {
                return;
            }

            ModuleAlertEventArgs args = new ModuleAlertEventArgs(
                module.id,
                module.name,
                alertType,
                message);

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                EventHandler<ModuleAlertEventArgs> callback =
                    (EventHandler<ModuleAlertEventArgs>)subscriber;

                try
                {
                    callback(this, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONITOR] Subscriber error: {ex.Message}");
                }
            }
        }

        /// Manually raises a maintenance-required event.
        public void RaiseMaintenanceRequired(Module module, string reason)
        {
            EventHandler<MaintenanceRequiredEventArgs> handler = MaintenanceRequired;

            if (handler == null)
            {
                return;
            }

            MaintenanceRequiredEventArgs args = new MaintenanceRequiredEventArgs(module, reason);

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                EventHandler<MaintenanceRequiredEventArgs> callback =
                    (EventHandler<MaintenanceRequiredEventArgs>)subscriber;

                try
                {
                    callback(this, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONITOR] Subscriber error: {ex.Message}");
                }
            }
        }
    }
}
