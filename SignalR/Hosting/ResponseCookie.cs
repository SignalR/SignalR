using System;

namespace SignalR.Hosting
{
    public class ResponseCookie : Cookie
    {
        public ResponseCookie(string name, string value, string domain = "", string path = "", bool secure = false, bool httpOnly = false, DateTime expires = default(DateTime))
            : base(name, value, domain, path)
        {
            Secure = secure;
            HttpOnly = httpOnly;
            Expires = expires;
        }

        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }
        public DateTime Expires { get; set; }
    }
}
