﻿using Microsoft.Data.SqlClient;
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
    public class DeclineSupersededUpdates : WsusStep
    {
        public override bool ShouldRun()
        {
            if (!wsusConfig.Steps.WsusSteps["DeclineSupersededUpdates"])
            {
                return false;
            }

            return true;
        }

        // WSUS Code based on https://github.com/proxb/PoshWSUS
        // WSUS Decline based on PS script https://docs.microsoft.com/en-us/troubleshoot/mem/configmgr/wsus-maintenance-guide
        public override Result Run()
        {
            var exclusionPeriod = TimeSpan.FromDays(10);
            var segmentDeclines = true;
            var segmentSize = TimeSpan.FromDays(31);
            var startDate = new DateTime(2015, 1, 1);
            var wsusServer = GetAdminConsole();
            var messages = new Dictionary<ResultMessageType, IList<string>>();


            // Microsoft.UpdateServices.Administration.UpdateCollection allUpdates = null;
            try
            {
                Console.WriteLine("Getting Updates");
                var searchScope = new UpdateScope();
                // searchScope.ApprovedStates = ApprovedStates.NotApproved | ApprovedStates.LatestRevisionApproved | ApprovedStates.HasStaleUpdateApprovals;
                if (segmentDeclines)
                {
                    Console.WriteLine("Declining Superseded Update - By Sections");

                    // All Updates By Week
                    var currentDate = startDate;
                    while (currentDate < DateTime.Now)
                    {
                        Console.WriteLine("Declining Superseded Update Segment- {0} - {1}", currentDate.ToString("yyyy-MM-dd"), currentDate.Add(segmentSize).ToString("yyyy-MM-dd"));
                        try
                        {
                            searchScope.FromCreationDate = currentDate;
                            // I Dont Know if the to/from is inclusive, so to ensure we cover that date, add 1 Day to the To
                            searchScope.ToCreationDate = currentDate.Add(segmentSize).AddDays(1);
                            var allUpdates = wsusServer.GetUpdates(searchScope);
                            foreach (var update in allUpdates.Cast<IUpdate>().Where(u => !u.IsDeclined).ToList())
                            {
                                if (!update.IsDeclined && update.IsSuperseded)
                                {
                                    if (update.CreationDate < DateTime.UtcNow.Add(exclusionPeriod))
                                    {
                                        try
                                        {
                                            Console.WriteLine("Declining Superseded Update - {0}", update.Description);
                                            update.Decline();
                                        }
                                        catch (Exception e)
                                        {
                                            throw;
                                            // Failed to decline update, should log it
                                            messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                                            return new Result(false, messages);
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            currentDate = currentDate.Add(segmentSize);
                        }
                    }
                }
                else
                {
                    // All Updates
                    Console.WriteLine("Declining Superseded Update - All");

                    var allUpdates = wsusServer.GetUpdates(searchScope);
                    foreach (var update in allUpdates.Cast<IUpdate>().Where(u => !u.IsDeclined).ToList())
                    {
                        if (!update.IsDeclined && update.IsSuperseded)
                        {
                            if (update.CreationDate < DateTime.UtcNow.Add(exclusionPeriod))
                            {
                                try
                                {
                                    Console.WriteLine("Declining Superseded Update - {0}", update.Description);
                                    update.Decline();
                                }
                                catch (Exception e)
                                {
                                    throw;
                                    // Failed to decline update, should log it
                                    messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                                    return new Result(false, messages);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw;
                messages.Add(ResultMessageType.Error, new List<string>() { e.Message, e.InnerException?.Message });
                return new Result(false, messages);
            }

            return new Result(true, messages);
        }
    }
}
