using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance
{
    public delegate void WriteLogLineHandler(string format, params object[] values);
    public interface IStep
    {
        void SetConfig(WSUSMaintenance.NerdleConfigs.WsusMaintenanceConfiguration config);
        bool ShouldRun();
        // For Future use; allows for Transaction encasing everything
        //Result Run(SqlConnection sqlConnection,SqlTransaction sqlTransaction);
        Result Run();

        event WriteLogLineHandler WriteLog;
    }
}
