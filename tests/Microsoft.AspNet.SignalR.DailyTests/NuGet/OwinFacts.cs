using System.Collections.Generic;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.NuGet
{
    public class OwinFacts
    {
        public static IEnumerable<string[]> Feeds
        {
            get
            {
                return new List<string[]>()
                {
                    new []{"OwinTeamCityDev"},
                    new []{"OwinTeamCityRelease"},
                    new []{"aspnetwebstacknightly"},
                    new []{"aspnetwebstacknightlyrelease"},
                    new []{"Staging"},
                    new []{"Production"},
                };
            }
        }        

        [Theory]
        [PropertyData("Feeds")]
        public void OwinCors(string name)
        {
            CommonNuGet.Run("Microsoft.Owin.Cors", name);
        }
    }
}
