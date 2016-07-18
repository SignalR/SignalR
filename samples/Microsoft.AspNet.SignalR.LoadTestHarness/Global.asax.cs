// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Web.Routing;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Global : System.Web.HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);
            RouteTable.Routes.MapConnection<TestConnection>("TestConnection", "TestConnection");
            RouteTable.Routes.MapHubs();

            Dashboard.Init();
        }
    }
}
