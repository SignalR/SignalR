// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;
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
        [MemberData(nameof(Feeds))]
        public void OwinCors(string name)
        {
            CommonNuGet.Run("Microsoft.Owin.Cors", name);
        }
    }
}
