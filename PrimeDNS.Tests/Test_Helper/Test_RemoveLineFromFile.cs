namespace PrimeDNS.Tests.Test_Helper
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Helper;
    using System.IO;

    [TestClass]
    public class TestRemoveLineFromFile
    {
        
        [TestMethod]
        public void TestRemoveLineFromFile_removeLineInput_removeLineOutput()
        {
            /*
             *  The following assignments are made to trigger the constructor of the respective classes. It won't work without that.
             */
            var primeDns = new PrimeDns();
            var config = new TestAppConfig();
            var hostFileUpdater = new HostFile.HostFileUpdater();

            var filePath = config.PrimeDnsTestsFiles + "test";
            var inputFilePath = config.PrimeDnsTestsFiles + "removeLineInput";
            var outputFilePath = config.PrimeDnsTestsFiles + "removeLineOutput";

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Copy(inputFilePath, filePath);
            FileHelper.RemoveLineFromFile(filePath, "###---LINE-TO-REMOVE");

            Assert.IsTrue(FileComparisonHelper.FilesAreEqual(filePath, outputFilePath));
        }
        
    }
}
