// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;
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
        [MemberData(nameof(Feeds))]
        public void SignalR(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRClient(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Client", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRCore(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Core", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRJS(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.JS", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRRedis(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Redis", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRSample(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Sample", name);
        }

        // Fails to install with "External packages cannot depend on packages that target projects."
        // [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRSelfHost(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SelfHost", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRServiceBus(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.ServiceBus", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRSqlServer(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SqlServer", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRSystemWeb(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SystemWeb", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRUtils(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Utils", name);
        }
    }
}
