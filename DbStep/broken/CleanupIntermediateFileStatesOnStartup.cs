using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    public class CleanupIntermediateFileStatesOnStartup : IStep
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
                WriteLine("Cleanup Intermediate File States On Startup - {0}", wsusConfig.WsusServerFQDN);
                using (var dbconnection = new SqlConnection(wsusConfig.Database?.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"CleanupIntermediateFileStatesOnStartup - TSQL - {e.Source}-{e.Message}");
                    };

                    dbconnection.Open();
                    var cmd = dbconnection.CreateCommand();
                    cmd.CommandText = "EXEC spCleanupIntermediateFileStatesOnStartup";
                    cmd.CommandTimeout = 0;
                    // This 
                    var param = new SqlParameter("@serverName", System.Data.SqlDbType.NVarChar);
                    param.SqlValue = wsusConfig.WsusServerFQDN;
                    cmd.Parameters.Add(param);
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

        public event WriteLogLineHandler WriteLog;

        private void WriteLine(string format, params object[] values)
        {
            if (WriteLog != null)
            {
                WriteLog(format, values);
            }
        }

        public bool ShouldRun()
        {
            return true;
        }

        public Result Run(SqlConnection sqlConnection, SqlTransaction sqlTransaction)
        {
            throw new NotImplementedException();
        }
    }
}
