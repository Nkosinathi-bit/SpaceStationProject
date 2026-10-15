using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRG_Project
{
    public interface ILog
    {
        string LogCategory { get; }
        string ToLogString();
    }
}
