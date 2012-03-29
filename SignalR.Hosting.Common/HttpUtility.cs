using System;
using System.Collections.Specialized;
using SignalR.Infrastructure;

namespace SignalR.Hosting.Common
{
    public static class HttpUtility
    {
        public static NameValueCollection ParseDelimited(string s)
        {
            var nvc = new NameValueCollection();
            if (s == null)
            {
                return nvc;
            }

            foreach (var pair in s.Split('&'))
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
                nvc[key] = UriQueryUtility.UrlDecode(value);
            }

            return nvc;
        }
    }
}
