/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Helper
{
    using System.Net;

    internal class IpHelper
    {
        public static bool IsIpAddressValid(string pIpAddress)
        {
            return IPAddress.TryParse(pIpAddress, out _);
        }
    }
}
