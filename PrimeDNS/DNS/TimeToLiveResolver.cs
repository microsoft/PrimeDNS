namespace PrimeDNS.DNS
{
    using global::DNS.Client;
    using global::DNS.Protocol;
    using global::DNS.Protocol.ResourceRecords;
    using PrimeDNS.Map;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    internal class TimeToLiveResolver
    {
        public static async Task<Tuple<PrimeDnsMapRow,bool>> TtlResolve(PrimeDnsMapRow pMapRow)
        {
            ClientRequest request = new ClientRequest(PrimeDns.Config.DnsResolver);

            // Requesting NS for the domain
            request.Questions.Add(new Question(Domain.FromString(pMapRow.HostName), RecordType.NS));
            request.RecursionDesired = false;

            int ttl = 300;

            try
            {
                var response = await request.Resolve();

                // Get all the Nameservers for the domain
                IList<IPAddress> nameServers = response.AnswerRecords
                    .Where(r => r.Type == RecordType.NS)
                    .Cast<IPAddressResourceRecord>()
                    .Select(r => r.IPAddress)
                    .ToList();

                foreach(IPAddress ip in nameServers)
                {
                    ClientRequest ttlRequest = new ClientRequest(ip);

                    // Requesting NS for the domain
                    ttlRequest.Questions.Add(new Question(Domain.FromString(pMapRow.HostName), RecordType.A));
                    ttlRequest.RecursionDesired = false;


                    var ttlResponse = await ttlRequest.Resolve();

                    IList<IResourceRecord> recordList = ttlResponse.AnswerRecords;

                    foreach (IResourceRecord r in recordList)
                    {
                        if (ttl > r.TimeToLive.Seconds)
                        {
                            ttl = r.TimeToLive.Seconds;
                        }
                    }
                }

                //PrimeDns.Log._LogInformation("Ttl Resolved successfully for Domain - " + pMapRow.HostName, Logger.Logger.CDnsResolver, null);
                Telemetry.Telemetry.PushDnsCallsData(pMapRow.HostName, "Success", "TtlResolver", 0, 0, "SUCCESS");
                pMapRow.TimeToLiveInSeconds = ttl;
                return Tuple.Create(pMapRow,true);
            }
            catch (Exception e)
            {
                PrimeDns.Log._LogError("Error occured while Ttl resolution for Domain - " + pMapRow.HostName, Logger.Logger.CDnsResolver, e);
                Telemetry.Telemetry.PushDnsCallsData(pMapRow.HostName, "Failure", "TtlResolver", 0, 0, e.Message);
                return Tuple.Create(pMapRow, false);
            }
        }

    }
}
