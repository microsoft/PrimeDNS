namespace PrimeDNS.Tests.Test_PrimeDNS
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using DNS;
    using Map;
    using System;
    using System.Threading;

    [TestClass]
    public class TestDnsResolver
    {
        [TestMethod]
        public void TestDnsResolveSuccess()
        {
            /*
             *  The following assignments is made to trigger the constructor of PrimeDNS class. It won't work without that.
             */
            var primeDns = new PrimeDns();

            var mapRow = new PrimeDnsMapRow("www.bing.com");
            var result = Tuple.Create(mapRow, false);

            var source = new CancellationTokenSource();
            var token = source.Token;

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
        public void TestDnsResolveNxDomain()
        {
            /*
             *  The following assignments is made to trigger the constructor of PrimeDNS class. It won't work without that.
             */
            var primeDns = new PrimeDns();

            var mapRow = new PrimeDnsMapRow("www.start.binginternal.com");
            var result = Tuple.Create(mapRow, false);

            var source = new CancellationTokenSource();
            var token = source.Token;

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
            /*
             *  The following assignments is made to trigger the constructor of PrimeDNS class. It won't work without that.
             */
            var primeDns = new PrimeDns();

            var mapRow = new PrimeDnsMapRow("www.dev5-bing-int1.com");
            var result = Tuple.Create(mapRow, false);

            var source = new CancellationTokenSource();
            var token = source.Token;

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
