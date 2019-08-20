namespace PrimeDNS.Map
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    /*
     * Describes the template for each row on the PrimeDNS MAP.
     */
    internal class PrimeDnsMapRow
    {
        public string HostName { get; set; }
        public IList<IPAddress> IpAddressList { get; set; }
        public DateTime LastUpdatedTime { get; set; }
        public DateTime LastCheckedTime { get; set; }
        public int TimeToLiveInSeconds;

        public PrimeDnsMapRow(string pHostName)
        {
            HostName = pHostName;
            IpAddressList = new List<IPAddress>();
            LastUpdatedTime = DateTime.Now;
            LastCheckedTime = DateTime.Now;
            TimeToLiveInSeconds = PrimeDns.Config.DefaultTimeToLiveInSeconds;
        }

        /*
         * IpAddressList is stored as a string in PrimeDNSMap Table (#-Seperated).
         * Hence, we require GetStringOfIpAddressList() for the convertion of types.
         */
        public string GetStringOfIpAddressList()
        {
            var stringBuilder = new StringBuilder("");
            foreach(var ip in IpAddressList)
            {
                stringBuilder.Append(ip.ToString()+"#");
            }
            var ipAddressListString = stringBuilder.ToString().TrimEnd('#');
            return ipAddressListString;
        }

        public void GetIpAddressListOfString(string pIpList)
        {
            this.IpAddressList.Clear();
            var ipList = pIpList.Split('#');
            foreach(string ip in ipList)
            {
                this.IpAddressList.Add(IPAddress.Parse(ip));
            }
        }
    }

}
