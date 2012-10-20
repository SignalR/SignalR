// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System.Security.Cryptography;
    using System.Text;

    static class Md5Hash
    {
        public static int Compute32bitHashCode(string data)
        {
            // TODO: Investigate if it is better to have a single instance and lock it to use it concurrently.
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] md5Hash = md5.ComputeHash(bytes);

                return
                    HashCode.Combine(
                        HashCode.Combine(md5Hash[0], md5Hash[1], md5Hash[2], md5Hash[3], md5Hash[4], md5Hash[5], md5Hash[6], md5Hash[7]),
                        HashCode.Combine(md5Hash[8], md5Hash[9], md5Hash[10], md5Hash[11], md5Hash[12], md5Hash[13], md5Hash[14], md5Hash[15]));
            }
        }
    }
}
