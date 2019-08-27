/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Tests.Test_MapUpdater
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestIsDomainNameValid
    {
        [TestMethod]
        public void TestIsDomainNameValid_ValidString_True()
        {
            Assert.IsTrue(DomainsConfig.IsDomainNameValid("www.bing.com"));
            Assert.IsTrue(DomainsConfig.IsDomainNameValid("www.tutorialspoint.com"));
        }

        [TestMethod]
        public void TestIsDomainNameValid_InvalidString_False()
        {
            Assert.IsFalse(DomainsConfig.IsDomainNameValid("PLEASEWORK"));
            Assert.IsFalse(DomainsConfig.IsDomainNameValid("123..com"));
        }

    }
}
