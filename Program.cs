﻿using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSUSMaintenance.DbStep;
using WSUSMaintenance.OtherStep;
using Serilog;

namespace WSUSMaintenance
{
    class Program
    {
        static void Main(string[] args)
        {
            var steps = new IStep[]
            {
                new BackupDatabase(),
                new CustomIndexes(),
                new OptimizeDatabase(),
                new DeclineSupersededUpdates(),
                new DeclineExpiredUpdates(),
                new CleanupObsoleteUpdates(),
                new CleanupObsoleteComputers(),
                new CompressUpdates(),
                new CleanupUnneededContentFiles(),
              };

            var wsusConfig = Nerdle.AutoConfig.AutoConfig.Map<NerdleConfigs.WsusMaintenanceConfiguration>();
            // get temp File for Log
            var tempFile = System.IO.Path.GetTempFileName();
            using (var log = new LoggerConfiguration()
                     .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                     .WriteTo.File(tempFile, Serilog.Events.LogEventLevel.Information, encoding: Encoding.UTF8)
                     .CreateLogger())
            {
                try
                {
                    for (int i = 0; i < steps.Length; i++)
                    {
                        log.Information("Checking Step {0}/{1} - {2}", (i + 1), steps.Length, steps[i].GetType().Name);
                      
                        // Set Config including Db Connection
                        steps[i].SetConfig(wsusConfig);

                        // set Logger
                        steps[i].WriteLog += (string format, object[] values) =>
                        {
                            log.Information(format, values);
                        };

                        if (steps[i].ShouldRun())
                        {
                            log.Information("Executing Step {0}/{1} - {2}", (i + 1), steps.Length, steps[i].GetType().Name);
                            var result = steps[i].Run();
                            if (!result.Success)
                            {
                                log.Error("Step {0} Failed; bailing", steps[i].GetType().Name);
                                var messages = new List<String>();
                                if (result.Messages?.ContainsKey(ResultMessageType.Error) ?? false)
                                {
                                    messages.AddRange(result.Messages[ResultMessageType.Error]);
                                }

                                foreach (var m in messages)
                                {
                                    log.Error(m);
                                }

                                throw new InvalidOperationException(messages.FirstOrDefault() ?? "Exception During Step");
                            }
                        }
                        else
                        {
                            log.Information("Skipping Step {0}/{1} - {2}", (i + 1), steps.Length, steps[i].GetType().Name);
                        }

                        log.Information("Finished Step {0}/{1} - {2}", (i + 1), steps.Length, steps[i].GetType().Name);
                    }
                }
                catch (Exception e)
                {
                    log.Error(e,"Error Running Wsus Maintenance");
                }

                // Close the logger, so we can re-open the file in SendCompletionEmail
            }

            // Send Completion Email
            var Email = new SendEmail();
            Email.SendCompletionEmail(wsusConfig, tempFile);          
        }
    }
}
