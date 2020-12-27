using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    public class CustomIndexes : IStep
    {
        private readonly string SqlCommand = @"
            -- Create custom index in tbLocalizedPropertyForRevision
            USE [SUSDB]

            CREATE NONCLUSTERED INDEX [nclLocalizedPropertyID] ON [dbo].[tbLocalizedPropertyForRevision]
            (
                 [LocalizedPropertyID] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

            -- Create custom index in tbRevisionSupersedesUpdate
            CREATE NONCLUSTERED INDEX [nclSupercededUpdateID] ON [dbo].[tbRevisionSupersedesUpdate]
            (
                 [SupersededUpdateID] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]";

        private readonly string SqlCheckCommand = @"
                SELECT Count(*) 
                FROM sys.indexes 
                WHERE name='nclLocalizedPropertyID' AND object_id = OBJECT_ID('[dbo].[tbLocalizedPropertyForRevision]')
            ";


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
                WriteLine("Running Sql Index Creation Script");
                using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"CustomIndexes - TSQL - {e.Source}-{e.Message}");
                    };

                    dbconnection.Open();
                    var cmd = dbconnection.CreateCommand();
                    cmd.CommandText = SqlCommand;
                    cmd.CommandTimeout = 0;

                    cmd.ExecuteNonQuery();
                }
                return new Result(true, new Dictionary<ResultMessageType, IList<string>>());
            }
            catch (Exception e)
            {
                var messages = new Dictionary<ResultMessageType, IList<string>>();
                messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                return new Result(false, messages);
            }
        }

        public bool ShouldRun()
        {
            if (string.IsNullOrWhiteSpace(wsusConfig?.Database?.ConnectionString))
            {
                return false;
            }

            using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
            {
                dbconnection.Open();
                var cmd = dbconnection.CreateCommand();
                cmd.CommandText = SqlCheckCommand;
                int.TryParse(cmd.ExecuteScalar().ToString(), out int result);
                return result == 0;
            }
        }

        public Result Run(SqlConnection sqlConnection, SqlTransaction sqlTransaction)
        {
            throw new NotImplementedException();
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
