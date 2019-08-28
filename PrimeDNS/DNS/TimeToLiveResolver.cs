/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.DNS
{
    using global::DNS.Client;
    using global::DNS.Protocol;
    using global::DNS.Protocol.ResourceRecords;
    using Map;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    internal class TimeToLiveResolver
    {
        public static async Task<Tuple<PrimeDnsMapRow,bool>> TtlResolve(PrimeDnsMapRow pMapRow)
        {
            var request = new ClientRequest(PrimeDns.Config.DnsResolver);

            // Requesting NS for the domain
            request.Questions.Add(new Question(Domain.FromString(pMapRow.HostName), RecordType.NS));
            request.RecursionDesired = false;

            var ttl = 300;

            try
            {
                var response = await request.Resolve();

                // Get all the NS for the domain
                IList<IPAddress> nameServers = response.AnswerRecords
                    .Where(r => r.Type == RecordType.NS)
                    .Cast<IPAddressResourceRecord>()
                    .Select(r => r.IPAddress)
                    .ToList();

                foreach(var ip in nameServers)
                {
                    var ttlRequest = new ClientRequest(ip);

                    // Requesting NS for the domain
                    ttlRequest.Questions.Add(new Question(Domain.FromString(pMapRow.HostName), RecordType.A));
                    ttlRequest.RecursionDesired = false;


                    var ttlResponse = await ttlRequest.Resolve();

                    var recordList = ttlResponse.AnswerRecords;

                    ttl = recordList.Select(r => r.TimeToLive.Seconds).Concat(new[] {ttl}).Min();
                }

                //PrimeDns.Log._LogInformation("Ttl Resolved successfully for Domain - " + pMapRow.HostName, Logger.Logger.CDnsResolver, null);
                Telemetry.Telemetry.PushDnsCallsData(pMapRow.HostName, "Success", "TtlResolver", 0, 0, "SUCCESS");
                pMapRow.TimeToLiveInSeconds = ttl;
                return Tuple.Create(pMapRow,true);
            }
            catch (Exception e)
            {
                PrimeDns.Log._LogError("Error occured while Ttl resolution for Domain - " + pMapRow.HostName, Logger.Logger.ConstDnsResolver, e);
                Telemetry.Telemetry.PushDnsCallsData(pMapRow.HostName, "Failure", "TtlResolver", 0, 0, e.Message);
                return Tuple.Create(pMapRow, false);
            }
        }

    }
}
