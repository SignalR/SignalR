using System;

namespace SignalR.Client.Hubs
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class HubAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        private readonly string _type;

        // This is a positional argument
        public HubAttribute(string type)
        {
            _type = type;
        }

        public string Type
        {
            get { return _type; }
        }
    }
}
