namespace PrimeDNS.Tests.Test_Helper
{
    using System.IO;

    class FileComparisonHelper
    {
        public static bool FilesAreEqual(string p_first, string p_second)
        {
            string s1 = File.ReadAllText(p_first);
            string s2 = File.ReadAllText(p_second);

            s1 = s1.Replace("\r", "");
            s2 = s2.Replace("\r", "");

            bool result = s2.Equals(s1);
            if (result)
                return true;
            return false;
        }
    }
}
