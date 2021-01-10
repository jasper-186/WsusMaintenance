using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.NerdleConfigs
{
    public enum BackupType
    {
        None = 0,
        File = 1,
        Rolling = 2
    }
    public class DatabaseConfiguration
    {
        public bool BackupFirst { get; set; } = true;
        public BackupType BackupType { get; set; } = BackupType.File;

        // Backup Location That can be reached by Sql/WID Server
        public string BackupFileName { get; set; }
        public string BackupRollingFolderPath { get; set; }

        public string ConnectionString { get; set; }
    }
}
