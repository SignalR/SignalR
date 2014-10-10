using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace Microsoft.AspNet.SignalR.Crank
{
	class DefaultConnectionFactory : IConnectionFactory
	{
		public Connection CreateConnection(string url)
		{
			return new Connection(url);
		}
	}
}
