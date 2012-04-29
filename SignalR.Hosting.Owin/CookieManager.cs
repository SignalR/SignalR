using System;

namespace SignalR.Hosting.Owin
{
    // TODO: Add support for cookies in OWIN when Gate adds it
    internal class CookieManager : IRequestCookieCollection
    {
        public Cookie this[string name]
        {
            get { return null; }
        }

        public int Count
        {
            get { return 0; }
        }
    }
}
