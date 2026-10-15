using System;

namespace PRG_Project
{
    /// Payload for alert events raised by the background monitor.
    internal class ModuleAlertEventArgs : EventArgs
    {
        public string ModuleId { get; }
        public string ModuleName { get; }
        public string AlertType { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public ModuleAlertEventArgs(string moduleId, string moduleName, string alertType, string message)
        {
            ModuleId = moduleId;
            ModuleName = moduleName;
            AlertType = alertType;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }


    /// Payload for maintenance-required events.
    internal class MaintenanceRequiredEventArgs : EventArgs
    {
        public string ModuleId { get; }
        public string ModuleName { get; }
        public string MaintenanceReason { get; }
        public DateTime Timestamp { get; }
        public Module Module { get; }

        public MaintenanceRequiredEventArgs(Module module, string reason)
        {
            Module = module;
            ModuleId = module.id;
            ModuleName = module.name;
            MaintenanceReason = reason;
            Timestamp = DateTime.Now;
        }
    }
}