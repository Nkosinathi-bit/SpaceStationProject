using System;

namespace PRG_Project
{
 
    /// Base exception for logical module/station errors.
    internal class ModuleOperationException : Exception
    {
        public string ModuleId { get; }
        public string ModuleName { get; }

        public ModuleOperationException(string moduleId, string moduleName, string message)
            : base(message)
        {
            ModuleId = moduleId;
            ModuleName = moduleName;
        }

        public ModuleOperationException(string moduleId, string moduleName, string message, Exception innerException)
            : base(message, innerException)
        {
            ModuleId = moduleId;
            ModuleName = moduleName;
        }
    }

    /// Thrown when an operation would exceed a station resource limit.
    internal class ResourceLimitExceededException : ModuleOperationException
    {
        public ResourceLimitExceededException(string moduleId, string moduleName, string message)
            : base(moduleId, moduleName, message)
        {
        }
    }

    /// Thrown when the station enters a logically invalid or critical state.
    internal class CriticalSystemStateException : ModuleOperationException
    {
        public CriticalSystemStateException(string moduleId, string moduleName, string message)
            : base(moduleId, moduleName, message)
        {
        }
    }
}