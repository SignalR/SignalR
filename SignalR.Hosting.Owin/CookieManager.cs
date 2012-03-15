using System;

namespace SignalR.Hosting.Owin
{
    // TODO: Add support for cookies in OWIN when Gate adds it
    internal class CookieManager : IRequestCookieCollection, IResponseCookieCollection
    {
        Cookie IRequestCookieCollection.this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        ResponseCookie IResponseCookieCollection.this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        void IResponseCookieCollection.Add(ResponseCookie cookie)
        {
            throw new NotImplementedException();
        }

        void IResponseCookieCollection.Clear()
        {
            throw new NotImplementedException();
        }
    }
}
