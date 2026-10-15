using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRG_Project
{
    public interface IMaintenance
    {
        bool Diagnostic();

        void Repair(int repairValue);

        bool requiresMaintenance {  get; } 
    }
}
