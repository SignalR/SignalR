using System;
using System.Collections.Generic;
using System.Web;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Chat.ContentProviders
{
    public class YouTubeContentProvider : EmbedContentProvider
    {
        public override string MediaFormatSting
        {
            get
            {
                return @"<object width=""425"" height=""344""><param name=""movie"" value=""http://www.youtube.com/v/{0}?fs=1""</param><param name=""allowFullScreen"" value=""true""></param><param name=""allowScriptAccess"" value=""always""></param><embed src=""http://www.youtube.com/v/{0}?fs=1"" type=""application/x-shockwave-flash"" allowfullscreen=""true"" allowscriptaccess=""always"" width=""425"" height=""344""></embed></object>";
            }
        }

        public override IEnumerable<string> Domains
        {
            get { yield return "http://www.youtube.com"; }
        }

        protected override IEnumerable<object> ExtractParameters(Uri responseUri)
        {
            var queryString = HttpUtility.ParseQueryString(responseUri.Query);
            string videoId = queryString["v"];
            if (!String.IsNullOrEmpty(videoId))
            {
                yield return videoId;
            }
        }
    }
}