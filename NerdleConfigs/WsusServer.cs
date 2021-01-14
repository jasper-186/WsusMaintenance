using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.NerdleConfigs
{
   public class WsusServerConfig
    {
        public string ServerName { get; set; }
        public int ServerPort { get; set; }
        public bool UseSSL { get; set; }
    }
}
