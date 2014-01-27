using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class TypeWithDateAsString
    {
        public string DateAsString { get; set; }
    }

    public class DateAsStringHub : Hub
    {
        public TypeWithDateAsString Invoke(TypeWithDateAsString value)
        {
            Clients.Caller.Callback(value);
            return value;
        }
    }
}
