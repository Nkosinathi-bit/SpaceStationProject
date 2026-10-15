using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRG_Project
{
    internal class StationLogger
    {
        private readonly string logFilePath;
        private readonly List<string> logBuffer = new List<string>();
        private readonly object bufferLock = new object();

        public StationLogger(string logFilePath = "station_log.txt")
        {
            this.logFilePath = logFilePath;
        }

        public void AttachToEngine(SimulationEngine engine)
        {
            if (engine == null) 
                return;

            engine.AlertRaised += OnAlertRaised;
            engine.MaintenanceRequired += OnMaintenanceRequired;
        }

        public void DetachFromEngine(SimulationEngine engine)
        {
            if (engine == null)
                return;

            engine.AlertRaised -= OnAlertRaised;
            engine.MaintenanceRequired -= OnMaintenanceRequired;
        }

        private void OnAlertRaised(object sender, ModuleAlertEventArgs e)
        {
            string line = $"{e.Timestamp: yyyy-MM-dd HH:mm:ss}, ALERT, {e.AlertType}, {e.ModuleId}, {e.ModuleName}, {e.Message}";
            AddToBuffer(line);
        }

        private void OnMaintenanceRequired(object sender, MaintenanceRequiredEventArgs e)
        {
            string line = $"{e.Timestamp:yyyy-MM-dd HH:mm:ss}, MAINTENANCE, {e.ModuleId}, {e.ModuleName}, {e.MaintenanceReason}";
            AddToBuffer(line);
        }

        private void AddToBuffer (string line)
        {
            lock (bufferLock)
            {
                logBuffer.Add(line);
            }
        }

        public void LogModuleSnapshot(IEnumerable<Module> modules)
        {
            if (modules == null)
                return;

            foreach (Module module in modules)
            {
                if (module is ILog loggable)
                {
                    string line = $"{loggable.LogCategory}, {loggable.ToLogString()}";
                    AddToBuffer(line);
                }
            }
        }

        public void SaveToFile()
        {
            try
            {
                lock (bufferLock)
                {
                    if (logBuffer.Count == 0)
                    {
                        Console.WriteLine("LOGGER! No new log entries to save");
                        return;
                    }

                    File.AppendAllLines(logFilePath, logBuffer);
                    Console.WriteLine($"LOGGER! {logBuffer.Count} entries written to {logFilePath}");
                    logBuffer.Clear();
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"LOGGER! Failed to write log file: {ex.Message}");
            }
        }

        public void PrintBufferedEntries()
        {
            lock (bufferLock)
            {
                if (logBuffer.Count == 0)
                {
                    Console.WriteLine("LOGGER! No new log entries");
                    return;
                }

                foreach (string entry in logBuffer)
                {
                    Console.WriteLine(entry);
                }
            }
        }
    }
}
