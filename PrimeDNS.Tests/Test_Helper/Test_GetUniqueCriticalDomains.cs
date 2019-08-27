namespace PrimeDNS.Tests.Test_Helper
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;
    using PrimeDNS.Helper;
    using System.Linq;

    [TestClass]
    public class TestGetUniqueCriticalDomains
    {
        [TestMethod]
        public void TestUniqueCriticalDomains()
        {
            JToken[] input = { "www.bing.com", "www.goodreads.com", "www.bing.com", "www.dhamma.org" };
            JToken[] expectedOutput = { "www.bing.com", "www.goodreads.com", "www.dhamma.org" };

            var actualOutput = JsonHelper.GetUniqueCriticalDomains(input);
            var isEqual = expectedOutput.SequenceEqual(actualOutput);

            Assert.IsTrue(isEqual);
        }
    }
}
