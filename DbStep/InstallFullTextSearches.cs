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
    public class InstallFullTextSearches : IStep
    {
        private WsusMaintenanceConfiguration wsusConfig { get; set; }

        public void SetConfig(WsusMaintenanceConfiguration config)
        {
            wsusConfig = config;
        }

        public Result Run()
        {
            var step = new InstallTitleFullTextSearch();
            if (step.ShouldRun())
            {
                WriteLog("Installing Title Full Text Search");
                var result = step.Run();
                if (!result.Success)
                {
                    WriteLog("Failed to install Title Full Text Search");
                }
            }

           var step2 = new InstallDescriptionFullTextSearch();
            if (step2.ShouldRun())
            {
                WriteLog("Installing Description Full Text Search");
                var result = step2.Run();
                if (!result.Success)
                {
                    WriteLog("Failed to install Description Full Text Search");
                }
            }

            var messages = new Dictionary<ResultMessageType, IList<string>>();            
            return new Result(true, messages);
        }

        public bool ShouldRun()
        {
            if (!wsusConfig.Steps.DatabaseSteps["InstallFullTextSearches"])
            {
                return false;
            }

            return true;            
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
