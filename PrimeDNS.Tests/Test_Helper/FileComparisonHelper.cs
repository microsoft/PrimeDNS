namespace PrimeDNS.Tests.Test_Helper
{
    using System.IO;

    internal class FileComparisonHelper
    {
        public static bool FilesAreEqual(string pFirst, string pSecond)
        {
            var s1 = File.ReadAllText(pFirst);
            var s2 = File.ReadAllText(pSecond);

            s1 = s1.Replace("\r", "");
            s2 = s2.Replace("\r", "");

            var result = s2.Equals(s1);
            return result;
        }
    }
}
