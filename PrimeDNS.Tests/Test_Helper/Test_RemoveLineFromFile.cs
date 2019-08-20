namespace PrimeDNS.Tests.Test_Helper
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PrimeDNS.Helper;
    using System.IO;

    [TestClass]
    public class Test_RemoveLineFromFile
    {
        /*
        [TestMethod]
        public void TestRemoveLineFromFile_removeLineInput_removeLineOutput()
        {
            PrimeDns primeDns = new PrimeDns();
            Test_AppConfig config = new Test_AppConfig();
            HostFile.HostFileUpdater hostFileUpdater = new HostFile.HostFileUpdater();

            string filePath = config.primeDNSTestsFiles + "test";
            string inputFilePath = config.primeDNSTestsFiles + "removeLineInput";
            string outputFilePath = config.primeDNSTestsFiles + "removeLineOutput";

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Copy(inputFilePath, filePath);
            FileHelper.RemoveLineFromFile(filePath, "###---LINE-TO-REMOVE");

            Assert.IsTrue(Test_Helper.FileComparisonHelper.FilesAreEqual(filePath, outputFilePath));
        }
        */
    }
}
