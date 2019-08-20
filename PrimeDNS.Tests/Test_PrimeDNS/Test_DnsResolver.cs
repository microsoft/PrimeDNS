namespace PrimeDNS.Tests.Test_PrimeDNS
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;
    using PrimeDNS.DNS;
    using PrimeDNS.Helper;
    using PrimeDNS.Map;
    using System;
    using System.Linq;
    using System.Threading;

    [TestClass]
    public class TestDnsResolver
    {
        [TestMethod]
        public void TestDnsResolveSuccess()
        {
            PrimeDns primeDns = new PrimeDns();
            PrimeDnsMapRow mapRow = new PrimeDnsMapRow("www.bing.com");
            Tuple<PrimeDnsMapRow, bool> result = Tuple.Create(mapRow, false);

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            try
            {
                result = DnsResolver.DnsResolve(mapRow, token).Result;
            }
            catch (Exception e)
            {
                throw new AssertFailedException(e.Message);
            }

            Assert.IsTrue(result.Item2);
        }

        [TestMethod]
        public void TestDnsResolveNXDomain()
        {
            PrimeDns primeDns = new PrimeDns();
            PrimeDnsMapRow mapRow = new PrimeDnsMapRow("www.start.binginternal.com");
            Tuple<PrimeDnsMapRow, bool> result = Tuple.Create(mapRow, false);

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            try
            {
                result = DnsResolver.DnsResolve(mapRow, token).Result;
            }
            catch (Exception e)
            {
                throw new AssertFailedException(e.Message);
            }

            Assert.IsFalse(result.Item2);
        }

        
        [TestMethod]
        public void TestDnsResolveServFail()
        {
            PrimeDns primeDns = new PrimeDns();
            PrimeDnsMapRow mapRow = new PrimeDnsMapRow("www.dev5-bing-int1.com");
            Tuple<PrimeDnsMapRow, bool> result = Tuple.Create(mapRow, false);

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            try
            {
                result = DnsResolver.DnsResolve(mapRow, token).Result;
            }
            catch (Exception e)
            {
                throw new AssertFailedException(e.Message);
            }

            Assert.IsFalse(result.Item2);
        }
        
    }
}
