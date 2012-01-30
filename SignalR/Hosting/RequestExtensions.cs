using System;

namespace SignalR.Hosting
{
    internal static class RequestExtensions
    {
        /// <summary>
        /// Gets a value from the QueryString, and if it's null or empty, gets it from the Form instead.
        /// </summary>
        public static string QueryStringOrForm(this IRequest request, string key)
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
