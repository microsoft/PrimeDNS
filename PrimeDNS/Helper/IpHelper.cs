namespace PrimeDNS.Helper
{
    using System.Net;

    class IpHelper
    {
        public static bool IsIpAddressValid(string pIpAddress)
        {
            return IPAddress.TryParse(pIpAddress, out IPAddress ipAddress);
        }
    }
}
