// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;
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
        [MemberData(nameof(Feeds))]
        public void SignalRi18n(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.i18n", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalR(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.cs", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRClient(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Client.cs", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRCore(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Core.cs", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRRedis(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.Redis.cs", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRServiceBus(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.ServiceBus.cs", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRSqlServer(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SqlServer.cs", name);
        }

        [Theory]
        [MemberData(nameof(Feeds))]
        public void SignalRSystemWeb(string name)
        {
            CommonNuGet.Run("Microsoft.AspNet.SignalR.SystemWeb.cs", name);
        }
    }
}
