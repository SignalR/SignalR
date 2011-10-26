using System;

namespace SignalR.Client.Hubs
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class HubMethodAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        private readonly string _method;

        // This is a positional argument
        public HubMethodAttribute(string method)
        {
            _method = method;
        }

        public string Method
        {
            get { return _method; }
        }
    }
}
