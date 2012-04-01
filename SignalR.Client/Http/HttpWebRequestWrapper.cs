using System.Net;

namespace SignalR.Client.Http
{
    public class HttpWebRequestWrapper : IRequest
    {
        private readonly HttpWebRequest _request;

        public HttpWebRequestWrapper(HttpWebRequest request)
        {
            _request = request;
        }

        public string UserAgent
        {
            get
            {
#if NETFX_CORE
                return _request.Headers[HttpRequestHeader.UserAgent];
#else
                return _request.UserAgent;
#endif
            }
            set
            {
#if NETFX_CORE
                // TODO: Figure out how to set the user agent in WINRT
                // _request.Headers[HttpRequestHeader.UserAgent] = value;
#else
                _request.UserAgent = value;
#endif
            }
        }

        public ICredentials Credentials
        {
            get
            {
                return _request.Credentials;
            }
            set
            {
                _request.Credentials = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return _request.CookieContainer;
            }
            set
            {
                _request.CookieContainer = value;
            }
        }

        public string Accept
        {
            get
            {
                return _request.Accept;
            }
            set
            {
                _request.Accept = value;
            }
        }

        public void Abort()
        {
            _request.Abort();
        }
    }
}
