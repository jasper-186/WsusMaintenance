using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.NerdleConfigs
{
    public class DatabaseConfiguration    
    {
        public bool BackupFirst { get; set; } = true;
        // Backup Location on Sql/WID Server
        public string BackupLocation { get; set; }
        public string ConnectionString { get; set; }
    }
}
