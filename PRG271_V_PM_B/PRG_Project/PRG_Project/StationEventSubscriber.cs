using System;
using System.Collections.Generic;
using System.Linq;

namespace PRG_Project
{

    /// Subscribes to monitor events and performs decoupled system responses.
    internal sealed class StationEventSubscriber
    {
        private readonly List<string> _history = new List<string>();
        private readonly object _historyLock = new object();


        /// Thread-safe copy of recent event history for later UI/logging use.
        public IReadOnlyList<string> History
        {
            get
            {
                lock (_historyLock)
                {
                    return _history.ToList();
                }
            }
        }

        public void OnAlertRaised(object sender, ModuleAlertEventArgs e)
        {
            string line =
                $"{e.Timestamp:HH:mm:ss} | ALERT | {e.ModuleName} ({e.ModuleId}) | " +
                $"{e.AlertType} | {e.Message}";

            // Alerts are still printed immediately because they are less frequent
            // and represent important system changes.
            lock (_historyLock)
            {
                _history.Add(line);
            }

            Console.WriteLine(line);
        }

        public void OnMaintenanceRequired(object sender, MaintenanceRequiredEventArgs e)
        {
            string line =
                $"{e.Timestamp:HH:mm:ss} | MAINT | {e.ModuleName} ({e.ModuleId}) | " +
                $"{e.MaintenanceReason}";

            // Always store in history for later review.
            lock (_historyLock)
            {
                _history.Add(line);
            }

            // Only print to console if the module is in a serious state,
            // otherwise avoid flooding the menu with routine maintenance messages.
            bool shouldPrint = e.Module.Integrity < 20 || e.Module.Status == ModuleStatus.Offline;

            if (shouldPrint)
            {
                Console.WriteLine(line);
            }

            // Decoupled automatic response using the IMaintenance contract.
            if (e.Module is IMaintenance maintainable)
            {
                try
                {
                    maintainable.Repair(15);

                    // Bring offline modules back online after auto-repair.
                    if (e.Module.Status == ModuleStatus.Offline && e.Module.Integrity > 0)
                    {
                        e.Module.Status = ModuleStatus.Online;
                        Console.WriteLine($"  => {e.ModuleId} restored to Online.");
                    }

                    string response =
                        $"  => Auto-repair +15 dispatched to {e.ModuleId}.";

                    lock (_historyLock)
                    {
                        _history.Add(response);
                    }

                    if (shouldPrint)
                    {
                        Console.WriteLine(response);
                    }
                }
                catch (Exception ex)
                {
                    string failure =
                        $"  => Auto-repair failed for {e.ModuleId}: {ex.Message}";

                    lock (_historyLock)
                    {
                        _history.Add(failure);
                    }

                    if (shouldPrint)
                    {
                        Console.WriteLine(failure);
                    }
                }
            }
        }
    }
}