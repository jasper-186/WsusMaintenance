using Microsoft.Data.SqlClient;
using Microsoft.UpdateServices.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WSUSMaintenance.WsusStep
{
    [Obsolete("Kept For Future code Reference")]
    public class CleanupObsoleteComputers //: IStep
    {
        public bool ShouldRun()
        {
            return true;
        }

        // WSUS Code based on https://github.com/proxb/PoshWSUS
        // WSUS Decline based on PS script https://docs.microsoft.com/en-us/troubleshoot/mem/configmgr/wsus-maintenance-guide
        public Result Run(SqlConnection dbConnection)
        {
            Console.WriteLine("Connecting to WSUS AdminProxy");
            var serverName = "localhost";
            var useSSL = false;
            var port = 8530;
            var exclusionPeriod = TimeSpan.FromDays(10);
            //var sslPort = 8531;

            LoadDlls();

            // Get the WSUS Server
            var WsusServer = Microsoft.UpdateServices.Administration.AdminProxy.GetUpdateServer(serverName, useSSL, port);
            if (WsusServer == null)
            {
                throw new Exception("failed to connect to WSUS Server");
            }

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

        private void LoadDlls()
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Select(i => i.GetName()).Where(n => n.Name == "Microsoft.UpdateServices.Administration").Any())
            {
                // These are already loaded and dont need reloaded
                return;
            }

            Assembly a;
            try
            {
                a = Assembly.Load("Microsoft.UpdateServices.Administration");
            }
            catch (Exception e)
            {
                if (Environment.Is64BitProcess)
                {
                    a = Assembly.Load(@"Libraries\x64\Microsoft.UpdateServices.Administration.dll");
                }
                else
                {
                    a = Assembly.Load(@"Libraries\x86\Microsoft.UpdateServices.Administration.dll");
                }
            }

            if (!AppDomain.CurrentDomain.GetAssemblies().Select(i => i.GetName()).Where(n => n.Name == "Microsoft.UpdateServices.Administration").Any())
            {
                throw new System.IO.FileLoadException("Failed to Load WSUS Assemblies");
            }
        }
    }
}
