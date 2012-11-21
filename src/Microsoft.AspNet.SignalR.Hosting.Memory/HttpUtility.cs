// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Specialized;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hosting.Common
{
    public static class HttpUtility
    {
        public static NameValueCollection ParseDelimited(string delimited)
        {
            var nvc = new NameValueCollection();
            if (delimited == null)
            {
                return nvc;
            }

            foreach (var pair in delimited.Split('&'))
            {
                var kvp = pair.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (kvp.Length == 0)
                {
                    continue;
                }

                string key = kvp[0].Trim();
                if (String.IsNullOrEmpty(key))
                {
                    continue;
                }
                string value = kvp.Length > 1 ? kvp[1].Trim() : null;
                nvc[key] = UrlDecoder.UrlDecode(value);
            }

            return nvc;
        }
    }
}
