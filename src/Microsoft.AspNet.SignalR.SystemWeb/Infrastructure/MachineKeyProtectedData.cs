using System;
using System.Net;
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

            return MachineKeyProtectedData40.Protect(unprotectedBytes);
        }

        public string Unprotect(string protectedValue, string purpose)
        {
            byte[] unprotectedBytes = MachineKeyProtectedData40.Unprotect(protectedValue);

            return _encoding.GetString(unprotectedBytes);
        }

        private static class MachineKeyProtectedData40
        {
            private const uint MagicHeader = 0x855d4ec9;

            public static string Protect(byte[] data)
            {
                byte[] dataWithHeader = new byte[data.Length + 4];
                Buffer.BlockCopy(data, 0, dataWithHeader, 4, data.Length);
                unchecked
                {
                    dataWithHeader[0] = (byte)(MagicHeader >> 24);
                    dataWithHeader[1] = (byte)(MagicHeader >> 16);
                    dataWithHeader[2] = (byte)(MagicHeader >> 8);
                    dataWithHeader[3] = (byte)(MagicHeader);
                }

                string hex = MachineKey.Encode(dataWithHeader, MachineKeyProtection.All);
                return HexToBase64(hex);
            }

            public static byte[] Unprotect(string protectedData)
            {
                string hex = Base64ToHex(protectedData);
                byte[] dataWithHeader = MachineKey.Decode(hex, MachineKeyProtection.All);

                if (dataWithHeader == null || dataWithHeader.Length < 4 || (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(dataWithHeader, 0)) != MagicHeader)
                {
                    // the decoded data is blank or doesn't begin with the magic header
                    return null;
                }

                byte[] retVal = new byte[dataWithHeader.Length - 4];
                Buffer.BlockCopy(dataWithHeader, 4, retVal, 0, retVal.Length);
                return retVal;
            }

            // String transformation helpers

            internal static string Base64ToHex(string base64)
            {
                StringBuilder builder = new StringBuilder((int)(base64.Length * 1.5));
                foreach (byte b in HttpServerUtility.UrlTokenDecode(base64))
                {
                    builder.Append(HexDigit(b >> 4));
                    builder.Append(HexDigit(b & 0x0F));
                }
                string result = builder.ToString();
                return result;
            }

            private static char HexDigit(int value)
            {
                return (char)(value > 9 ? value + '7' : value + '0');
            }

            private static int HexValue(char digit)
            {
                return digit > '9' ? digit - '7' : digit - '0';
            }

            internal static string HexToBase64(string hex)
            {
                int size = hex.Length / 2;
                byte[] bytes = new byte[size];
                for (int idx = 0; idx < size; idx++)
                {
                    bytes[idx] = (byte)((HexValue(hex[idx * 2]) << 4) + HexValue(hex[(idx * 2) + 1]));
                }
                string result = HttpServerUtility.UrlTokenEncode(bytes);
                return result;
            }
        }
    }
}
