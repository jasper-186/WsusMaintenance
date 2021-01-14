using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    public class DeclineExpiredUpdates : IStep
    {
        private WsusMaintenanceConfiguration wsusConfig { get; set; }

        public void SetConfig(WsusMaintenanceConfiguration config)
        {
            wsusConfig = config;
        }

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
                WriteLine("Declining Expired Updates");
                using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"DeclineExpiredUpdates - TSQL - {e.Source}-{e.Message}");
                    };
                    dbconnection.Open();
                    var cmd = dbconnection.CreateCommand();
                    cmd.CommandText = "EXEC spDeclineExpiredUpdates";
                    cmd.CommandTimeout = 0;
                    var username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    cmd.Parameters.Add(new SqlParameter("adminName", username));
                    cmd.ExecuteNonQuery();
                }
                return new Result(true, new Dictionary<ResultMessageType, IList<string>>());
            }
            catch (Exception e)
            {
                throw;
                var messages = new Dictionary<ResultMessageType, IList<string>>();
                messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                return new Result(false, messages);
            }
        }

        public bool ShouldRun()
        {
            if (!wsusConfig.Steps.DatabaseSteps["DeclineExpiredUpdates"])
            {
                return false;
            }

            return true;
        }

     
        public event WriteLogLineHandler WriteLog;

        private void WriteLine(string format, params object[] values)
        {
            if (WriteLog != null)
            {
                WriteLog(format, values);
            }
        }
    }
}
