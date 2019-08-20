namespace PrimeDNS.Tests
{
    using System.IO;
    using Microsoft.Extensions.Configuration;
    class Test_AppConfig
    {
        public string primeDNSTestsHome;
        public string primeDNSTestsFiles;

        public static IConfiguration Configuration { get; set; }
        public Test_AppConfig()
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory() + "//..//..//..//Test_Files//")
                    .AddJsonFile("TestAppSettings.json");

            Configuration = builder.Build();

            primeDNSTestsHome = Configuration["PrimeDNSTestsHome"];
            primeDNSTestsFiles = primeDNSTestsHome + "\\Test_Files\\";
        }
    }
}
