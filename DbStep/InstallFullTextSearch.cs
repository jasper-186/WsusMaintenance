using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.DbStep
{
    // Full text Search for use with 
    //      Decline Itanium Updates
    //      Decline Surface Updates

    // Head up this is a bit Janky, because you cant create a Full text index without the name of the primary keys index
    public class InstallFullTextSearch : IStep
    {

        private readonly string isFullTextInstalledSqlCommand = @"SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')";
        private readonly string isFullTextEnabledSqlCommand = @"SELECT is_fulltext_enabled FROM sys.databases WHERE database_id = DB_ID()";
        private readonly string enableFullTextSqlCommand = @"exec sp_fulltext_database 'enable';";
        private readonly string getFullTextCatalogSqlCommand = @"Select TOP 1 [name] FROM sys.fulltext_catalogs Order By is_default DESC, fulltext_catalog_id ASC";
        private readonly string createFullTextCatalogSqlCommand = @"CREATE FULLTEXT CATALOG SUSDBCatalog AS DEFAULT;";

        private readonly string getPrimaryKeyNameSqlCommand = @"SELECT 
                                                                   TOP 1
                                                                   i.name    
                                                                FROM sys.indexes i
                                                                    inner join sys.index_columns ic  ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                                                                    inner join sys.columns c ON ic.object_id = c.object_id AND c.column_id = ic.column_id
                                                                WHERE i.is_primary_key = 1
                                                                    and i.object_ID = OBJECT_ID('dbo.tbXml');";



        private readonly string createFullTextSqlCommandTemplate = @"
	                                    CREATE FULLTEXT INDEX ON [dbo].[tbXml](
	                                    [RootElementXml] LANGUAGE 'English')
	                                    KEY INDEX {0} ON ({1}, FILEGROUP [PRIMARY])
	                                    WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM);";

        private readonly string SqlCheckCommand = @"
                SELECT Count(*)                 
                FROM sys.columns c 
                INNER JOIN sys.fulltext_index_columns fic 
                    ON c.object_id = fic.object_id 
                    AND c.column_id = fic.column_id
                WHERE 
	                c.object_id = OBJECT_ID('dbo.tbxml')
	                AND c.name = 'RootElementXml'
            ";


        private WsusMaintenanceConfiguration wsusConfig { get; set; }

        public void SetConfig(WsusMaintenanceConfiguration config)
        {
            wsusConfig = config;
        }

        public Result Run()
        {
            var messages = new Dictionary<ResultMessageType, IList<string>>();
            if (string.IsNullOrWhiteSpace(wsusConfig?.Database?.ConnectionString))
            {

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
                    // Is Full Text Installed?
                    var cmd = dbconnection.CreateCommand();
                    cmd.CommandText = isFullTextInstalledSqlCommand;
                    var isInstalled = (int)cmd.ExecuteScalar();

                    if (isInstalled != 1)
                    {
                        messages.Add(ResultMessageType.Warn, new List<string>() { "Full Text is not installed on database server" });
                        return new Result(true, messages);
                    }

                    // Is Full Text Enabled on this database?
                    cmd = dbconnection.CreateCommand();
                    cmd.CommandText = isFullTextEnabledSqlCommand;
                    var isEnabled = (bool)cmd.ExecuteScalar();

                    // Enable it if not
                    if (!isEnabled)
                    {
                        cmd = dbconnection.CreateCommand();
                        cmd.CommandText = enableFullTextSqlCommand;
                        cmd.ExecuteNonQuery();
                    }

                    // Does a catalog already Exist?
                    cmd = dbconnection.CreateCommand();
                    cmd.CommandText = getFullTextCatalogSqlCommand;
                    var catalogName = Convert.ToString(cmd.ExecuteScalar());
                    if (string.IsNullOrWhiteSpace(catalogName))
                    {
                        // Catalog doesnt Exists, Make one
                        cmd = dbconnection.CreateCommand();
                        cmd.CommandText = createFullTextCatalogSqlCommand;
                        cmd.ExecuteNonQuery();
                        // set the Catalog name to the one we created
                        catalogName = "SUSDBCatalog";
                    }

                    // Get the Primary Key of the table
                    cmd = dbconnection.CreateCommand();
                    cmd.CommandText = getPrimaryKeyNameSqlCommand;
                    var primaryKeyName = Convert.ToString(cmd.ExecuteScalar());

                    if (string.IsNullOrWhiteSpace(primaryKeyName))
                    {
                        messages.Add(ResultMessageType.Error, new List<string>() { "Failed to retrieve the primary key of tbXml" });
                        return new Result(false, messages);
                    }

                    // Create the Primary Key Sql
                    var createFullTextSqlCommand = string.Format(createFullTextSqlCommandTemplate, primaryKeyName, catalogName);

                    // create the full text index
                    cmd = dbconnection.CreateCommand();
                    cmd.CommandText = createFullTextSqlCommand;
                    cmd.ExecuteNonQuery();
                }

                return new Result(true, new Dictionary<ResultMessageType, IList<string>>());
            }
            catch (Exception e)
            {
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
                if (int.TryParse(cmd.ExecuteScalar().ToString(), out int result))
                {
                    return result == 0;
                }

                // we shouldn't defaultly run this
                return false;
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
