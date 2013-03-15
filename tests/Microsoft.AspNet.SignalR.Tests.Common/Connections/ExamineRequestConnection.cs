using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class ExamineRequestConnection : PersistentConnection
    {
        public override Task ProcessRequest(Hosting.HostContext context)
        {

            return base.ProcessRequest(context);
        }
    }
}
