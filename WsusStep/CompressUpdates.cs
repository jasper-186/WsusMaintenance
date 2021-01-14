using Microsoft.Data.SqlClient;
using Microsoft.UpdateServices.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.NerdleConfigs;

namespace WSUSMaintenance.WsusStep
{
    public class CompressUpdates : WsusStep
    {
        public override bool ShouldRun()
        {
            if (!wsusConfig.Steps.WsusSteps["CompressUpdates"])
            {
                return false;
            }

            return true;
        }

        // WSUS Code based on https://github.com/proxb/PoshWSUS
        // WSUS Decline based on PS script https://docs.microsoft.com/en-us/troubleshoot/mem/configmgr/wsus-maintenance-guide
        public override Result Run()
        {
            var wsusServer = GetAdminConsole();
            var messages = new Dictionary<ResultMessageType, IList<string>>();

            try
            {
                Console.WriteLine("Compressing Updates");
                var clnUpMngr = wsusServer.GetCleanupManager();
                var scope = new CleanupScope()
                {
                    CompressUpdates = true
                };

                clnUpMngr.ProgressHandler += ClnUpMngr_ProgressHandler;
                clnUpMngr.PerformCleanup(scope);
                return new Result(true, messages);
            }
            catch (Exception e)
            {
                // Failed to decline update, should log it
                messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                return new Result(false, messages);
            }
        }

        private void ClnUpMngr_ProgressHandler(object sender, CleanupEventArgs e)
        {
            Console.WriteLine("{0} - {1} - {2}", e.ProgressInfo, e.CurrentProgress, e.UpperProgressBound);
        }
    }
}
