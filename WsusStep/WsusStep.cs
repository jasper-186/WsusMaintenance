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
    public abstract class WsusStep : IStep
    {
        protected WsusMaintenanceConfiguration wsusConfig { get; set; }

        public event WriteLogLineHandler WriteLog;

        // public abstract event WriteLogLineHandler WriteLog;

        public void SetConfig(WsusMaintenanceConfiguration config)
        {
            wsusConfig = config;
        }

        // public abstract bool ShouldRun();
        //public abstract Result Run();

        public IUpdateServer GetAdminConsole()
        {
            var serverName = wsusConfig.Server?.ServerName;
            var useSSL = wsusConfig.Server?.UseSSL??true;
            var port = wsusConfig.Server?.ServerPort ?? 8530;
            var exclusionPeriod = TimeSpan.FromDays(10);
            
            //var sslPort = 8531;

            LoadDlls();

            // Get the WSUS Server
            var WsusServer = Microsoft.UpdateServices.Administration.AdminProxy.GetUpdateServer(serverName, useSSL, port);
            if (WsusServer == null)
            {
                throw new Exception("failed to connect to WSUS Server");
            }

            return WsusServer;
        }

        protected void LoadDlls()
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

        protected void WriteLine(string format, params object[] values)
        {
            if (WriteLog != null)
            {
                WriteLog(format, values);
            }
        }

        public abstract bool ShouldRun();
        public abstract Result Run();
    }
}
