using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SignalR.Infrastructure
{
    internal static class RequestHelpers
    {
        /// <summary>
        /// Gets a value from the QueryString, and if it's null or empty, gets it from the Form instead.
        /// </summary>
        public static string QueryStringOrForm(this HttpRequestBase request, string key)
        {
            var value = request.QueryString[key];
            if (String.IsNullOrEmpty(value))
            {
                value = request.Form[key];
            }
            return value;
        }
    }
}
