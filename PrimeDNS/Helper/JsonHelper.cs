namespace PrimeDNS.Helper
{
    using Newtonsoft.Json.Linq;
    using System.Linq;

    internal class JsonHelper
    {
        /*
         * GetUniqueCriticalDomains makes sure the JToken Array is unique by calling Distinct().
         * As further checks might be needed later, keeping this as a helper function might be useful.
         */
        public static JToken[] GetUniqueCriticalDomains(JToken[] pJTokenArray)
        {
            return pJTokenArray.Distinct().ToArray();
        }
    }
}
