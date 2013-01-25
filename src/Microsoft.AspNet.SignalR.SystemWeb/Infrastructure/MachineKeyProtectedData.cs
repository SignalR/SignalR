// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
        private const uint ConnectionTokenMagicHeader = 0x855d4ec9;
        private const uint GroupsMagicHeader = 0x85c8b45a;

        private static readonly UTF8Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public string Protect(string data, string purpose)
        {
            MachineKeyProtectedData40 protectedData = GetProtectedDataForPurpose(purpose);

            byte[] unprotectedBytes = _encoding.GetBytes(data);

            return protectedData.Protect(unprotectedBytes);
        }

        public string Unprotect(string protectedValue, string purpose)
        {
            MachineKeyProtectedData40 protectedData = GetProtectedDataForPurpose(purpose);

            byte[] unprotectedBytes = protectedData.Unprotect(protectedValue);

            return _encoding.GetString(unprotectedBytes);
        }

        private static MachineKeyProtectedData40 GetProtectedDataForPurpose(string purpose)
        {
            switch (purpose)
            {
                case Purposes.ConnectionToken:
                    return new MachineKeyProtectedData40(ConnectionTokenMagicHeader);
                case Purposes.Groups:
                    return new MachineKeyProtectedData40(GroupsMagicHeader);
            }

            throw new NotSupportedException();
        }

        private class MachineKeyProtectedData40
        {
            private readonly uint _magicHeader;

            public MachineKeyProtectedData40(uint magicHeader)
            {
                _magicHeader = magicHeader;
            }

            public string Protect(byte[] data)
            {
                byte[] dataWithHeader = new byte[data.Length + 4];
                Buffer.BlockCopy(data, 0, dataWithHeader, 4, data.Length);
                unchecked
                {
                    dataWithHeader[0] = (byte)(_magicHeader >> 24);
                    dataWithHeader[1] = (byte)(_magicHeader >> 16);
                    dataWithHeader[2] = (byte)(_magicHeader >> 8);
                    dataWithHeader[3] = (byte)(_magicHeader);
                }

                string hex = MachineKey.Encode(dataWithHeader, MachineKeyProtection.All);
                return HexToBase64(hex);
            }

            public byte[] Unprotect(string protectedData)
            {
                string hex = Base64ToHex(protectedData);
                byte[] dataWithHeader = MachineKey.Decode(hex, MachineKeyProtection.All);

                if (dataWithHeader == null || dataWithHeader.Length < 4 || (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(dataWithHeader, 0)) != _magicHeader)
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
