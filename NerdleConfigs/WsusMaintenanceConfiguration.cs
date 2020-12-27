using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.NerdleConfigs
{
    public class WsusMaintenanceConfiguration
    {
        //public string WsusDBConnectionString { get; set; }
        public string WsusServerFQDN { get; set; }
        public DatabaseConfiguration Database { get; set; }
        public SmtpConfiguration Smtp { get; set; }
    }
}
