using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    public class CompressUpdates : IStep
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
                WriteLine("Compress Updates");
                using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"CompressUpdates - TSQL - {e.Source}-{e.Message}");
                    };
                    dbconnection.Open();

                    var cmd = dbconnection.CreateCommand();
                    cmd.CommandText = "EXEC spGetUpdatesToCompress";
                    cmd.CommandTimeout = 0;
                    //  cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    var compressUpdateList = new List<long>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            if (long.TryParse(reader[0].ToString(), out long temp))
                            {
                                compressUpdateList.Add(temp);
                            }
                            else
                            {
                                WriteLine("Failed to Parse {0} into long", reader[0]);
                            }
                        }
                    }

                    WriteLine("Compressing {0} Updates", compressUpdateList.Count);

                    for (var i = 0; i < compressUpdateList.Count; i++)
                    {
                        var update = compressUpdateList[i];
                        WriteLine("Compressing {0} - {1}/{2}", update, i + 1, compressUpdateList.Count);
                        var compressCmd = dbconnection.CreateCommand();
                        compressCmd.CommandText = "EXEC spCompressUpdate @localUpdateID";
                        compressCmd.CommandTimeout = 0;
                        compressCmd.Parameters.Add(new SqlParameter("@localUpdateID", update));
                        compressCmd.ExecuteNonQuery();
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
