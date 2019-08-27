/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Tests.Test_HostFileUpdater
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class TestHostFileSectionUpdater
    {
        
        [TestMethod]
        public void TestHostFileSectionUpdater_hostsInput_hostsOutput()
        {
            /*
             *  The following assignments is made to trigger the constructor of PrimeDNS class. It won't work without that.
             */
            var primeDns = new PrimeDns();

            var config = new TestAppConfig();
            var hostFileUpdater = new HostFile.HostFileUpdater();

            var filePath = config.PrimeDnsTestsFiles + "test";
            var inputFilePath = config.PrimeDnsTestsFiles + "hostsInput";
            var outputFilePath = config.PrimeDnsTestsFiles + "hostsOutput";

            const string data = "127.0.0.1\twww.dhamma.org\n127.0.0.1\twww.bing.com\n127.0.0.1\twww.goodreads.com";

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Copy(inputFilePath, filePath);
            hostFileUpdater.RemoveOldPrimeDnsSectionEntries(filePath);
            hostFileUpdater.FindPrimeDnsSectionBegin(filePath);
            Helper.FileHelper.InsertIntoFile(filePath, data, hostFileUpdater.PrimeDnsBeginLine + 1);

            Assert.IsTrue(Test_Helper.FileComparisonHelper.FilesAreEqual(filePath, outputFilePath));
        }
        
    }
}
