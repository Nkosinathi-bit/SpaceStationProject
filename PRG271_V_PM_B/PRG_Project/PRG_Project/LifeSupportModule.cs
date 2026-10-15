using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PRG_Project
{
    internal class LifeSupportModule : Module, IMaintenance, ILog
    {
        private double oxygenLevel;
        private int currentCrewCount;
        public int maxCrewCount { get; }
        public double OxygenLevel 
        { 
            get => oxygenLevel; 
            set
            {
                if (value < 0) oxygenLevel = 0;
                else if (value > 100) oxygenLevel = 100;
                else oxygenLevel = value;

                if (oxygenLevel < 15.0 && Status == ModuleStatus.Online)
                    Console.WriteLine($"[WARNING] {name} ({id}) has critically low oxygen levels - {oxygenLevel:F1}%");
            }
        }
        public int CurrentCrewCount 
        { 
            get => currentCrewCount; 
            set
            {
                if (value < 0)
                    currentCrewCount = 0;
                else if (value > maxCrewCount)
                {
                    currentCrewCount = maxCrewCount;
                    Console.WriteLine($"[ALERT] {name} is currently at maximum capacity");
                }
                else
                    currentCrewCount = value;
            }
        }

        public LifeSupportModule(string name, double powerConsumption, int capacity): 
            base(name, powerConsumption)
        {
            maxCrewCount = capacity < 1 ? 1 : capacity;
            currentCrewCount = 0;
            oxygenLevel = 100.0;
        }

        public override string GetTelemetryReport()
        {
            return $"--- LIFE SUPPORT REPORT [{id}] ---\n" +
                   $"Name:       {name}\n" +
                   $"Status:     {status}\n" +
                   $"Integrity:  {Integrity}%\n" +
                   $"Power Draw: {PowerConsumption} kW\n" +
                   $"Crew:       {currentCrewCount} / {maxCrewCount} Astronauts\n" +
                   $"Oxygen:     {OxygenLevel:F1}%\n" +
                   $"---------------------------------";
        }
        public override void ProcessSimulationTick()
        {
            base.ProcessSimulationTick();

            if (status != ModuleStatus.Offline && status != ModuleStatus.Standby)
            {
                double consumption = currentCrewCount * 0.25;

                if (consumption == 0) consumption = 0.05;

                oxygenLevel -= consumption;
            }
            else if (status == ModuleStatus.Standby)
                oxygenLevel -= (currentCrewCount * 0.12);
        }

        public bool requiresMaintenance => Integrity < 50 || OxygenLevel < 30.0;

        public bool Diagnostic()
        {
            return Integrity > 70 && OxygenLevel > 50.0;
        }

        public void Repair(int repairValue)
        {
            Integrity += repairValue;
            OxygenLevel = 100.0;
        }

        public string LogCategory => "LIFE_SUPPORT";

        public string ToLogString()
        {
            return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{id},{status},{Integrity}%,O2:{OxygenLevel:F1}%" ;
        }
    }
}
