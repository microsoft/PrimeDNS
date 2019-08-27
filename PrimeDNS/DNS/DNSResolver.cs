/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

/*
 * Class that will do the DNS Resolving required by Map Updater.
 */
namespace PrimeDNS.DNS
{
    using DnsClient;
    using Map;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    internal class DnsResolver
    {
        public static async Task<Tuple<PrimeDnsMapRow,bool>> DnsResolve(PrimeDnsMapRow pMapRow, CancellationToken pToken)
        {
            var newlyAddedIpAddressCount = 0;
            var removedIpAddressCount = 0;
            var isChanged = false;
            var lookup = new LookupClient
            {
                UseCache = false
            };

            try
            {
                var ipToBeAdded = new List<IPAddress>();

                var result = await lookup.QueryAsync(pMapRow.HostName, QueryType.A, pToken);
                if(result.Header.ResponseCode == DnsResponseCode.NoError)
                {
                    pMapRow.LastCheckedTime = DateTime.Now;
                    var records = result.Answers.ARecords().Distinct();
                    var aRecords = records.ToList();
                    foreach (var ar in from ar in aRecords let flag = Enumerable.Contains(pMapRow.IpAddressList, ar?.Address) where !flag select ar)
                    {
                        ipToBeAdded.Add(ar?.Address);
                        pMapRow.LastUpdatedTime = DateTime.Now;
                        newlyAddedIpAddressCount++;
                    }

                    var ipToBeRemoved = new List<IPAddress>();

                    foreach (var ip in pMapRow.IpAddressList)
                    {
                        var flag = aRecords.Any(ar => ip.Equals(ar?.Address));
                        if (flag) continue;
                        ipToBeRemoved.Add(ip);
                        pMapRow.LastUpdatedTime = DateTime.Now;
                        removedIpAddressCount++;
                    }

                    foreach (var i in ipToBeAdded)
                    {
                        pMapRow.IpAddressList.Add(i);
                    }

                    foreach (var i in ipToBeRemoved)
                    {
                        pMapRow.IpAddressList.Remove(i);
                    }

                    Telemetry.Telemetry.PushDnsCallsData(pMapRow.HostName, "Success", "DnsResolver", newlyAddedIpAddressCount, removedIpAddressCount, "SUCCESS");
                    //PrimeDns.Log._LogInformation("Dns Resolver successful for domain - " + pMapRow.HostName, Logger.Logger.CDnsResolver, null);
                }
                else
                {
                    PrimeDns.Log._LogError("Dns Resolver Failed for domain - " + pMapRow.HostName +  "With Error " + result.Header.ResponseCode.ToString(), Logger.Logger.CDnsResolver, null);
                    Telemetry.Telemetry.PushDnsCallsData(pMapRow.HostName, "Success", "DnsResolver", newlyAddedIpAddressCount, removedIpAddressCount, result.Header.ResponseCode.ToString());
                    //PrimeDns.Log._LogInformation("Dns Resolver successful for domain with a non NoError Response Code - " + pMapRow.HostName, Logger.Logger.CDnsResolver, null);
                }

            }
            catch(Exception e)
            {
                PrimeDns.Log._LogError("Dns Resolver Failed for domain - " + pMapRow.HostName, Logger.Logger.CDnsResolver, e);
                Telemetry.Telemetry.PushDnsCallsData(pMapRow.HostName, "Failure","DnsResolver", newlyAddedIpAddressCount, removedIpAddressCount, e.Message);
            }
            if (newlyAddedIpAddressCount + removedIpAddressCount > 0)
                isChanged = true;
            return Tuple.Create(pMapRow,isChanged);
        }
    }
}
