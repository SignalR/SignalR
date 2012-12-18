using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class MyGroupConnection : PersistentConnection
    {
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            JObject operation = JObject.Parse(data);
            int type = operation.Value<int>("type");
            string group = operation.Value<string>("group");

            if (type == 1)
            {
                return Groups.Add(connectionId, group);
            }
            else if (type == 2)
            {
                return Groups.Remove(connectionId, group);
            }
            else if (type == 3)
            {
                return Groups.Send(group, operation.Value<string>("message"));
            }

            return base.OnReceivedAsync(request, connectionId, data);
        }
    }

}
