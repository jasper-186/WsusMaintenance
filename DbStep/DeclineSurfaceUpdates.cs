﻿using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.Helpers;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    // Idea Taken from 
    //   Fabian Niesen
    // Original Implementation
    //  https://www.infrastrukturhelden.de/microsoft-infrastruktur/wsus/windows-server-update-services-bereinigen.html (decline-WSUSUpdatesTypes.ps1) 
    public class DeclineSurfaceUpdates : IStep
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
                WriteLine("Decline Surface Updates");
                using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
                {
                    dbconnection.InfoMessage += (sender, e) =>
                    {
                        WriteLine($"DeclineSurfaceUpdates - TSQL - {e.Source}-{e.Message}");
                    };
                    dbconnection.Open();
                    var whereClause = FullTextWhereHelper.GetWhereClause(dbconnection, WhereClauseJoiner.AND, "Title", "Microsoft", "Surface");
                    var cmd = dbconnection.CreateCommand();
                    cmd.CommandText = @"  
                                        SELECT 
	                                        Distinct
	                                        U.[UpdateID]     
                                        FROM [dbo].[tbPreComputedLocalizedProperty] x
                                        JOIN [dbo].[tbUpdate] U ON U.UpdateID = X.UpdateID
                                        JOIN [dbo].[tbRevision] R ON U.LocalUpdateID = R.LocalUpdateID AND  X.RevisionID = R.RevisionID
									     where 
                                        -- If its hidden, its already declined
                                        U.IsHidden = 0
                                        AND" + whereClause;
                    cmd.CommandTimeout = 0;
                    var itaniumUpdatesList = new List<System.Guid>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var readerVal = reader[0].ToString();
                            if (string.IsNullOrWhiteSpace(readerVal)) continue;

                            if (Guid.TryParse(readerVal, out Guid localUpdateOid))
                            {
                                itaniumUpdatesList.Add(localUpdateOid);
                            }
                            else
                            {
                                WriteLine("Failed to parse UpdateId {0}", readerVal);
                            }
                        }
                    }

                    WriteLine("Execution Decline on {0} Updates", itaniumUpdatesList.Count);

                    for (var i = 0; i < itaniumUpdatesList.Count; i++)
                    {
                        try
                        {
                            var update = itaniumUpdatesList[i];
                            WriteLine("Decline Surface Update {0} - {1}/{2}", update, (i + 1), itaniumUpdatesList.Count);
                            var declineCmd = dbconnection.CreateCommand();
                            declineCmd.CommandText = "EXEC spDeclineUpdate @updateID, @adminName";

                            // it shouldn't take 2 Hours to decline an update, so its probaby deadlocked somewhere
                            declineCmd.CommandTimeout = (int)TimeSpan.FromHours(2).TotalSeconds;
                            declineCmd.Parameters.Add(new SqlParameter("@updateID", update));
                            declineCmd.Parameters.Add(new SqlParameter("@adminName", "WsusMaintenance"));
                            //deleteCmd.CommandType = System.Data.CommandType.StoredProcedure;
                            declineCmd.ExecuteNonQuery();
                        }
                        catch (TimeoutException)
                        {
                            WriteLine("Failed to Decline Update {0} - 2 Hour Timeout Expired; Moving on", itaniumUpdatesList[i]);
                        }
                    }
                }
                return new Result(true, new Dictionary<ResultMessageType, IList<string>>());
            }
            catch (Exception e)
            {
                throw;
                //var messages = new Dictionary<ResultMessageType, IList<string>>();
                //messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                //return new Result(false, messages);
            }
        }

        public bool ShouldRun()
        {
            if (!wsusConfig.Steps.DatabaseSteps["DeclineSurfaceUpdates"])
            {
                return false;
            }

            using (var dbconnection = new SqlConnection(wsusConfig.Database.ConnectionString))
            {
                dbconnection.InfoMessage += (sender, e) =>
                {
                    WriteLine($"DeclineSurfaceUpdates - TSQL - {e.Source}-{e.Message}");
                };

                dbconnection.Open();
                var whereClause = FullTextWhereHelper.GetWhereClause(dbconnection, WhereClauseJoiner.AND, "Title", "Microsoft", "Surface");

                var cmd = dbconnection.CreateCommand();
                cmd.CommandText = @"  
                                    SELECT 
	                                    TOP 1
                                        1 as Count
                                    FROM [dbo].[tbPreComputedLocalizedProperty] x
                                    JOIN [dbo].[tbUpdate] U ON U.UpdateID = X.UpdateID
                                    JOIN [dbo].[tbRevision] R ON U.LocalUpdateID = R.LocalUpdateID AND  X.RevisionID = R.RevisionID
									where 
                                    -- If its hidden, its already declined
                                    U.IsHidden = 0
                                    AND
                                    " + whereClause;
                cmd.CommandTimeout = 0;
                var result = Convert.ToInt32(cmd.ExecuteScalar());
                return result > 0;
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

        public Result Run(SqlConnection sqlConnection, SqlTransaction sqlTransaction)
        {
            throw new NotImplementedException();
        }
    }
}
