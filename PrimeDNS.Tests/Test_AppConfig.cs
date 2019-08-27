namespace PrimeDNS.Tests
{
    using System.IO;
    using Microsoft.Extensions.Configuration;
    class TestAppConfig
    {
        public string PrimeDnsTestsHome;
        public string PrimeDnsTestsFiles;

        public static IConfiguration Configuration { get; set; }
        public TestAppConfig()
        {
            PrimeDnsTestsFiles = Directory.GetCurrentDirectory() + "//..//..//..//Test_Files//";
        }
    }
}
