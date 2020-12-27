using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    public class BackupDatabase : IStep
    {
        private WsusMaintenanceConfiguration wsusConfig { get; set; }


        public Result Run()
        {
            if (string.IsNullOrWhiteSpace(wsusConfig?.Database?.ConnectionString))
            {
                var messages = new Dictionary<ResultMessageType, IList<string>>();
                messages.Add(ResultMessageType.Error, new List<string>() { "Config not set properly" });
                return new Result(false, messages);
            }

            try
            {
                WriteLine("Creating Connection");
                using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"BackupDatabase - TSQL - {e.Source}-{e.Message}");
                    };

                    var dbBakFileName = string.Format(@"C:\WSUSBackup\{0}_WSUS_SUSDB.bak", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"));
                    WriteLine("Creating Copy-Only Back up at {0}", dbBakFileName);

                    var temp = new ServerConnection(dbconnection);
                    Server server = new Server(temp);
                    Backup bkpDBFull = new Backup();
                    bkpDBFull.Action = BackupActionType.Database;
                    bkpDBFull.Database = "SUSDB";
                    bkpDBFull.Devices.AddDevice(dbBakFileName, DeviceType.File);
                    bkpDBFull.BackupSetName = "WSUSBackup";
                    bkpDBFull.BackupSetDescription = "WSUSBackup - Full Backup";
                    /* You can specify the expiration date for your backup data
                     * after that date backup data would not be relevant */
                    bkpDBFull.ExpirationDate = DateTime.MaxValue;

                    bkpDBFull.CopyOnly = true;
                    /* Wiring up events for progress monitoring */
                    bkpDBFull.PercentComplete += CompletionStatusInPercent;
                    bkpDBFull.Complete += Backup_Completed;

                    /* SqlBackup method starts to take back up
                     * You can also use SqlBackupAsync method to perform the backup
                     * operation asynchronously */
                    bkpDBFull.SqlBackup(server);
                    var messages = new Dictionary<ResultMessageType, IList<string>>();
                    return new Result(true, messages);
                }
            }
            catch (Exception e)
            {
                throw;
                var messages = new Dictionary<ResultMessageType, IList<string>>();
                messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                return new Result(false, messages);
            }
        }

        public event WriteLogLineHandler WriteLog;

        private void WriteLine(string format, params object[] values)
        {
            if (WriteLog != null)
            {
                WriteLog(format, values);
            }
        }

        private void CompletionStatusInPercent(object sender, PercentCompleteEventArgs args)
        {
            WriteLine("BackupDatabase - Percent completed: {0}%.", args.Percent);
        }

        private void Backup_Completed(object sender, ServerMessageEventArgs args)
        {
            WriteLine("BackupDatabase - Hurray...Backup completed.");
            WriteLine(args.Error.Message);
        }

        //private void Restore_Completed(object sender, ServerMessageEventArgs args)
        //{
        //    WriteLine("BackupDatabase - Hurray...Restore completed.");
        //    WriteLine(args.Error.Message);
        //}

        public void SetConfig(WsusMaintenanceConfiguration config)
        {
            wsusConfig = config;
        }

        public bool ShouldRun()
        {
            if (!wsusConfig?.Database?.BackupFirst ?? true)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(wsusConfig?.Database?.BackupLocation))
            {
                return false;
            }

            if (!System.IO.Directory.Exists(wsusConfig.Database.BackupLocation))
            {
                throw new InvalidOperationException(string.Format("Database Backup Location Does not exist![{0}]", wsusConfig.Database.BackupLocation));
            }

            // Default to backing up the database, since its just good planning
            return true;

        }
    }
}
