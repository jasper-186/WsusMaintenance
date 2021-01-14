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
    public class CleanupObsoleteComputers : WsusStep
    {
        public override bool ShouldRun()
        {
            if (!wsusConfig.Steps.WsusSteps["CleanupObsoleteComputers"])
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(wsusConfig.Server?.ServerName))
            {
                return false;
            }

            return true;
        }


        // WSUS Code based on https://github.com/proxb/PoshWSUS
        // WSUS Decline based on PS script https://docs.microsoft.com/en-us/troubleshoot/mem/configmgr/wsus-maintenance-guide
        public override Result Run()
        {
            WriteLine("Connecting to WSUS AdminProxy on {0}",wsusConfig.Server?.ServerName);
            
            // Get the WSUS Server
            var WsusServer = GetAdminConsole();
            var messages = new Dictionary<ResultMessageType, IList<string>>();

            try
            {
                Console.WriteLine("Cleaning up Obsolete Computers");
                var clnUpMngr = WsusServer.GetCleanupManager();
                var scope = new CleanupScope()
                {
                    CleanupObsoleteComputers = true
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

        //private void LoadDlls()
        //{
        //    if (AppDomain.CurrentDomain.GetAssemblies().Select(i => i.GetName()).Where(n => n.Name == "Microsoft.UpdateServices.Administration").Any())
        //    {
        //        // These are already loaded and dont need reloaded
        //        return;
        //    }

        //    Assembly a;
        //    try
        //    {
        //        a = Assembly.Load("Microsoft.UpdateServices.Administration");
        //    }
        //    catch (Exception e)
        //    {
        //        if (Environment.Is64BitProcess)
        //        {
        //            a = Assembly.Load(@"Libraries\x64\Microsoft.UpdateServices.Administration.dll");
        //        }
        //        else
        //        {
        //            a = Assembly.Load(@"Libraries\x86\Microsoft.UpdateServices.Administration.dll");
        //        }
        //    }

        //    if (!AppDomain.CurrentDomain.GetAssemblies().Select(i => i.GetName()).Where(n => n.Name == "Microsoft.UpdateServices.Administration").Any())
        //    {
        //        throw new System.IO.FileLoadException("Failed to Load WSUS Assemblies");
        //    }
        //}
    }
}
