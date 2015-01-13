using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class MyGroupConnection : PersistentConnection
    {
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            JObject operation = JObject.Parse(data);
            int type = operation.Value<int>("type");
            string group = operation.Value<string>("group");
            string groupConnectionId = operation.Value<string>("connectionId") ?? connectionId;

            if (type == 1)
            {
                return Groups.Add(groupConnectionId, group);
            }
            else if (type == 2)
            {
                return Groups.Remove(groupConnectionId, group);
            }
            else if (type == 3)
            {
                return Groups.Send(group, operation.Value<string>("message"));
            }

            return base.OnReceived(request, connectionId, data);
        }
    }

}
