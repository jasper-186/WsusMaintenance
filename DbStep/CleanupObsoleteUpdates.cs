using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    public class CleanupObsoleteUpdates : IStep
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
                WriteLine("Cleanup Obsolete Updates");
                using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"CleanupObsoleteUpdates - TSQL - {e.Source}-{e.Message}");
                    };
                    dbconnection.Open();
                    var cmd = dbconnection.CreateCommand();
                    cmd.CommandText = "EXEC spGetObsoleteUpdatesToCleanup";
                    cmd.CommandTimeout = 0;
                    var ObsoleteUpdateList = new List<int>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var readerVal = reader[0].ToString();

                            if (string.IsNullOrWhiteSpace(readerVal)) continue;

                            if (int.TryParse(readerVal, out int localUpdateOid))
                            {
                                ObsoleteUpdateList.Add(localUpdateOid);
                            }
                            else
                            {
                                WriteLine("Failed to parse localUpdateOid {0}", readerVal);
                            }
                        }
                    }

                    WriteLine("Execution Delete on {0} Updates", ObsoleteUpdateList.Count);

                    for (var i = 0; i < ObsoleteUpdateList.Count; i++)
                    {
                        try
                        {
                            var update = ObsoleteUpdateList[i];
                            WriteLine("Deleting Obsolete Update {0} - {1}/{2}", update, (i + 1), ObsoleteUpdateList.Count);
                            var deleteCmd = dbconnection.CreateCommand();
                            deleteCmd.CommandText = "EXEC spDeleteUpdate @localUpdateID";

                            // it shouldn't take 2 Hours to delete an update, so its probaby deadlocked somewhere
                            deleteCmd.CommandTimeout = (int)TimeSpan.FromHours(2).TotalSeconds;
                            deleteCmd.Parameters.Add(new SqlParameter("@localUpdateID", update));
                            //deleteCmd.CommandType = System.Data.CommandType.StoredProcedure;
                            deleteCmd.ExecuteNonQuery();
                        }
                        catch (TimeoutException)
                        {
                            WriteLine("Failed to Delete Update {0} - 2 Hour Timeout Expired; Moving on", ObsoleteUpdateList[i]);
                        }
                    }
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
        public Result Run(SqlConnection sqlConnection, SqlTransaction sqlTransaction)
        {
            throw new NotImplementedException();
        }
    }
}
