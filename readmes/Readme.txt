Please see http://go.microsoft.com/fwlink/?LinkId=272764 for more information on using SignalR.

Upgrading from 1.x to 2.0
-------------------------
Please see http://go.microsoft.com/fwlink/?LinkId=320578 for more information on how to 
upgrade your SignalR 1.x application to 2.0.

Mapping the Hubs connection
----------------------------
To enable SignalR in your application, create a class called Startup with the following:

using Microsoft.Owin;
using Owin;
using MyWebApplication;

namespace MyWebApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
} 

Getting Started
---------------
See http://www.asp.net/signalr/overview/getting-started for more information on how to get started.

Why does ~/signalr/hubs return 404 or Why do I get a JavaScript error: 'myhub is undefined'?
--------------------------------------------------------------------------------------------
This issue is generally due to a missing or invalid script reference to the auto-generated Hub JavaScript proxy at '~/signalr/hubs'.
Please make sure that the Hub route is registered before any other routes in your application.
 
In ASP.NET MVC 4 you can do the following:
 
      <script src="~/signalr/hubs"></script>
 
If you're writing an ASP.NET MVC 3 application, make sure that you are using Url.Content for your script references:
 
    <script src="@Url.Content("~/signalr/hubs")"></script>
 
If you're writing a regular ASP.NET application use ResolveClientUrl for your script references or register them via the ScriptManager 
using a app root relative path (starting with a '~/'):
 
    <script src='<%: ResolveClientUrl("~/signalr/hubs") %>'></script>
 
If the above still doesn't work, you may have an issue with routing and extensionless URLs. To fix this, ensure you have the latest 
patches installed for IIS and ASP.NET. 