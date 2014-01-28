using System.Collections.Generic;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.NuGet
{
    public class SignalRLocFacts
    {
        public static IEnumerable<string[]> Feeds
        {
            get
            {
                return new List<string[]>()
                {
                    new []{"aspnetwebstacknightly"},
                    new []{"aspnetwebstacknightlyrelease"},
                    new []{"Staging"},
                    new []{"Production"},
                };
            }
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRi18n(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.i18n", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalR(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.cs", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRClient(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Client.cs", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRCore(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Core.cs", name);
        }        

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRRedis(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Redis.cs", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRServiceBus(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.ServiceBus.cs", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRSqlServer(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SqlServer.cs", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRSystemWeb(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SystemWeb.cs", name);
        }
    }
}
