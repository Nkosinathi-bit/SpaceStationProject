using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PRG_Project
{
    internal class PowerGridModule : Module, IMaintenance, ILog
    {
        private double energyGeneration;
        private double storedEnergy;
        public bool isGridStable {  get; private set; }

        public double storageCapacity { get; }
        public double EnergyGeneration 
        { 
            get => energyGeneration;
            set
            { 
                energyGeneration = value < 0 ? 0 : value;
            }
        }
        public double StoredEnergy 
        { 
            get => storedEnergy; 
            set
            {
                if (value < 0)
                    storedEnergy = 0;
                else if (value > storageCapacity)
                    storedEnergy = storageCapacity;
                else
                    storedEnergy = value;
                isGridStable = energyGeneration >= PowerConsumption;
            }
        }

        public PowerGridModule(string name, double powerConsumption, double energyGeneration, double storageCapacity)
            : base (name, powerConsumption)
        {
            this.energyGeneration = energyGeneration;
            this.storageCapacity = storageCapacity < 100.0 ? 100.0 : storageCapacity;
            storedEnergy = storageCapacity * 0.75;
            isGridStable = true;
        }

        public override string GetTelemetryReport()
        {
            string gridStatus = isGridStable ? "STABLE" : "DRAINING";

            return $"--- POWER GRID REPORT [{id}] ---\n" +
                   $"Name:        {name}\n" +
                   $"Status:      {status}\n" +
                   $"Integrity:   {Integrity}%\n" +
                   $"Self Draw:   {PowerConsumption} kW\n" +
                   $"Generation:  {energyGeneration} kW\n" +
                   $"Reserves:    {storedEnergy:F1} / {storageCapacity:F1} kWh\n" +
                   $"Grid Status: {gridStatus}\n" +
                   $"---------------------------------";
        }

        public override void ProcessSimulationTick()
        {
            base.ProcessSimulationTick();

            if (status == ModuleStatus.Offline)
            {
                energyGeneration = 0;
                isGridStable = false;
                return;
            }
            double generation = energyGeneration;
            if (status == ModuleStatus.Damaged)
            {
                generation *= 0.60;
            }

            storedEnergy += generation;
        }

        public bool requiresMaintenance => Integrity < 50 || storedEnergy < (storageCapacity * 0.20);
        
        public bool Diagnostic()
        {
            return Integrity > 65 && isGridStable && status != ModuleStatus.Offline;
        }

        public void Repair(int repairValue)
        {
            Integrity += repairValue;

            storedEnergy += (storageCapacity * 0.25);

            if (status == ModuleStatus.Offline || status == ModuleStatus.Damaged)
            {
                status = ModuleStatus.Online;
            }
        }

        public string LogCategory => "POWER_GRID";

        public string ToLogString()
        {
            return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{id},{status},{Integrity}%,Batt:{storedEnergy:F1}kwh,Stable:{isGridStable}";
        }
    }
}
