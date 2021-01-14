using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.NerdleConfigs
{
    public class WsusStepConfiguration
    {
        public Dictionary<string,bool> DatabaseSteps { get; set; }
        public Dictionary<string, bool> WsusSteps { get; set; }
    }
}
