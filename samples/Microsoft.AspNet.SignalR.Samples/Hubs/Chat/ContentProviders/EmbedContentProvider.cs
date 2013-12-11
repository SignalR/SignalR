using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Chat.ContentProviders
{
    public abstract class EmbedContentProvider : IContentProvider
    {
        public virtual Regex MediaUrlRegex
        {
            get
            {
                return null;
            }
        }
        public abstract IEnumerable<string> Domains { get; }
        public abstract string MediaFormatSting { get; }

        protected virtual IEnumerable<object> ExtractParameters(Uri responseUri)
        {
            if (MediaUrlRegex != null)
            {
                return MediaUrlRegex.Match(responseUri.AbsoluteUri)
                                    .Groups
                                    .Cast<Group>()
                                    .Skip(1)
                                    .Select(g => g.Value)
                                    .Where(v => !String.IsNullOrEmpty(v));
            }
            return null;
        }

        public string GetContent(HttpWebResponse response)
        {
            if (Domains.Any(d => response.ResponseUri.AbsoluteUri.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
            {
                var args = ExtractParameters(response.ResponseUri);
                if (args == null || !args.Any())
                {
                    return null;
                }

                return String.Format(MediaFormatSting, args.ToArray());
            }
            return null;
        }
    }
}