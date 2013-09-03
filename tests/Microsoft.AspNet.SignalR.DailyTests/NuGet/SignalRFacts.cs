using System.Collections.Generic;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.NuGet
{
    public class SignalRFacts
    {
        public static IEnumerable<string[]> Feeds
        {
            get
            {
                return new List<string[]>()
                {
                    new []{"TeamCityDev"},
                    new []{"TeamCityRelease"},
                    new []{"aspnetwebstacknightly"},
                    new []{"aspnetwebstacknightlyrelease"},
                    new []{"Staging"},
                    new []{"Production"},
                };
            }
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalR(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRClient(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Client", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRCore(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Core", name);
        }       

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRJS(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.JS", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRRedis(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Redis", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRSample(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Sample", name);
        }

        // Fails to install with "External packages cannot depend on packages that target projects."
        // [Theory]
        [PropertyData("Feeds")]
        public void SignalRSelfHost(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SelfHost", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRServiceBus(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.ServiceBus", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRSqlServer(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SqlServer", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRSystemWeb(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SystemWeb", name);
        }

        [Theory]
        [PropertyData("Feeds")]
        public void SignalRUtils(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Utils", name);
        }        
    }
}
