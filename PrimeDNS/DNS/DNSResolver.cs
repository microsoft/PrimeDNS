/*
 * Class that will do the DNS Resolving required by Map Updater.
 */
 namespace PrimeDNS.DNS
{
    using DnsClient;
    using DnsClient.Protocol;
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
            int newlyAddedIpAddressCount = 0;
            int removedIpAddressCount = 0;
            bool isChanged = false;
            var lookup = new LookupClient();

            lookup.UseCache = false;


            try
            {
                List<IPAddress> IpToBeAdded = new List<IPAddress>();

                var result = await lookup.QueryAsync(pMapRow.HostName, QueryType.A, pToken);
                if(result.Header.ResponseCode == DnsResponseCode.NoError)
                {
                    pMapRow.LastCheckedTime = DateTime.Now;
                    var records = result.Answers.ARecords().Distinct();
                    foreach (ARecord ar in records)
                    {
                        bool flag = false;
                        foreach (IPAddress ip in pMapRow.IpAddressList)
                        {
                            if (ip.Equals(ar?.Address))
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            IpToBeAdded.Add(ar?.Address);
                            pMapRow.LastUpdatedTime = DateTime.Now;
                            newlyAddedIpAddressCount++;
                        }
                    }

                    List<IPAddress> IpToBeRemoved = new List<IPAddress>();

                    foreach (IPAddress ip in pMapRow.IpAddressList)
                    {
                        bool flag = false;
                        foreach (ARecord ar in records)
                        {
                            if (ip.Equals(ar?.Address))
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            IpToBeRemoved.Add(ip);
                            pMapRow.LastUpdatedTime = DateTime.Now;
                            removedIpAddressCount++;
                        }
                    }

                    foreach (IPAddress i in IpToBeAdded)
                    {
                        pMapRow.IpAddressList.Add(i);
                    }

                    foreach (IPAddress i in IpToBeRemoved)
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
