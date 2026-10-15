using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRG_Project
{
    enum ModuleStatus
    {
        Online, Standby, Damaged, Offline
    }
    internal abstract class Module
    {
        private int integrity;
        public string id { get; }
        public string name { get; }
        private double powerConsumption;
        public ModuleStatus status;

        public int Integrity { get => integrity; 
            set
            {
                //Limits value to between 0 and 100
                if (value < 0) integrity = 0;
                else if (value > 100) integrity = 100;
                else integrity = value;

                if (integrity == 0)
                {
                    status = ModuleStatus.Offline;
                    throw new Exception();
                }
                else if (integrity < 30 && status == ModuleStatus.Online)
                {
                    Status = ModuleStatus.Damaged;
                }
            }
        }
        public double PowerConsumption { get => powerConsumption; 
            set
            {
                powerConsumption = value < 0 ? 0 : value;
            }
        }
        public ModuleStatus Status { get => status; 
            set
            {
                if (status == value) return;

                ModuleStatus oldStatus = status;
                status = value;

                StatusChange(oldStatus, status);
            }
        }

        protected Module(string name, double powerConsumption)
        {
            this.name = name;
            this.powerConsumption = powerConsumption;

            string uniqueToken = Guid.NewGuid().ToString();
            id = "MOD" + uniqueToken.Substring(0, 4).ToUpper();

            integrity = 100;
            status = ModuleStatus.Online;
        }

        private void StatusChange(ModuleStatus oldStatus, ModuleStatus newStatus)
        {
            switch (newStatus)
            {
                case ModuleStatus.Offline:
                    Console.WriteLine($"[ALERT] {name} ({id}) is offline");
                    PowerConsumption = 0;
                    break;
                case ModuleStatus.Standby:
                    PowerConsumption *= 0.25;
                    break;
                case ModuleStatus.Damaged:
                    Console.WriteLine($"[WARNING] {name} ({id}) has been damaged and requires maintenance");
                    break;
                default: break;
            }
        }

        public abstract string GetTelemetryReport();

        public virtual void ProcessSimulationTick()
        {
            if (Status != ModuleStatus.Offline)
            {
                Integrity -= 1;
            }
        }
    }
}
