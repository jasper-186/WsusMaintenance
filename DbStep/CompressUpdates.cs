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
                WriteLine("Please Note; due to Update depencies this may take a couple rounds");
                using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"CompressUpdates - TSQL - {e.Source}-{e.Message}");
                    };
                    dbconnection.Open();

                    var compressUpdateList = GetCompressableUpdates(dbconnection);
                    int round = 1;
                    while (compressUpdateList.Count > 0)
                    {
                        var roundCount = compressUpdateList.Count();
                        WriteLine("Compressing {0} Updates - Round {1}", roundCount, round);
                        for (var i = 0; i < compressUpdateList.Count; i++)
                        {
                            var update = compressUpdateList.Dequeue();
                            WriteLine("Compressing {0} - {1}/{2} - Round {3}", update, i + 1, roundCount, round);
                            var compressCmd = dbconnection.CreateCommand();
                            compressCmd.CommandText = "EXEC spCompressUpdate @localUpdateID";
                            compressCmd.CommandTimeout = 0;
                            compressCmd.Parameters.Add(new SqlParameter("@localUpdateID", update));
                            compressCmd.ExecuteNonQuery();
                        }

                        if (round > 10)
                        {
                            WriteLine($"CompressUpdates - Round Count Excessive; Bailing to prevent non-breaking loop");
                            break;
                        }

                        compressUpdateList = GetCompressableUpdates(dbconnection);
                        round++;
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


        private Queue<long> GetCompressableUpdates(SqlConnection connection)
        {
            var compressUpdateList = new Queue<long>();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC spGetUpdatesToCompress";
            cmd.CommandTimeout = 0;
            //  cmd.CommandType = System.Data.CommandType.StoredProcedure;
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {

                    if (long.TryParse(reader[0].ToString(), out long temp))
                    {
                        compressUpdateList.Enqueue(temp);
                    }
                    else
                    {
                        WriteLine("Failed to Parse {0} into long", reader[0]);
                    }
                }
            }

            return compressUpdateList;
        }


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
