// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace Microsoft.AspNet.SignalR.Crank
{
	class DefaultConnectionFactory : IConnectionFactory
	{
		public Connection CreateConnection(string url)
		{
			return new Connection(StripQueryString(url), GetQueryStringFromUrl(url));
		}

        /// <summary>
        /// returns the url without the query and the query string as two seperate items
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Item1:The url without the query string
        /// Item2:The query string if there was one or null</returns>
        private static string GetQueryStringFromUrl(string url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            var firstIndex = url.IndexOf("?");
            return firstIndex > -1 ? url.Substring(firstIndex + 1) : null;
        }

        /// <summary>
        /// returns the url without the query string if one is present else it returns the original url
        /// </summary>
        /// <param name="url"></param>
        /// <returns>The url without the query string or the original url if no query string was present</returns>
        private static string StripQueryString(string url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            var firstIndex = url.IndexOf("?");
            return firstIndex > -1 ? url.Substring(0, firstIndex) : url;
        }
    }
}
