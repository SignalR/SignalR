// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Text;
using System.Web;
using System.Web.Security;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.SystemWeb.Infrastructure
{
    public class MachineKeyProtectedData : IProtectedData
    {
        private static readonly UTF8Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public string Protect(string data, string purpose)
        {
            byte[] unprotectedBytes = _encoding.GetBytes(data);

            byte[] protectedBytes = MachineKey.Protect(unprotectedBytes, purpose);

            return HttpServerUtility.UrlTokenEncode(protectedBytes);
        }

        public string Unprotect(string protectedValue, string purpose)
        {
            byte[] protectedBytes = HttpServerUtility.UrlTokenDecode(protectedValue);

            byte[] unprotectedBytes = MachineKey.Unprotect(protectedBytes, purpose);

            return _encoding.GetString(unprotectedBytes);
        }
    }
}
