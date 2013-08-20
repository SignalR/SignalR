Please see http://go.microsoft.com/fwlink/?LinkId=272764 for more information on using SignalR.

Upgrading from 1.x to 2.0
-------------------------
Please see http://go.microsoft.com/fwlink/?LinkId=320578 for more information on how to 
upgrade your SignalR 1.x application to 2.0.
 
Mapping the Hubs connection
----------------------------------------
SignalR Hubs will not work without a Hub route being configured. To register the default Hubs route, create a class called Startup 
with the signature below and call app.MapSignalR() in your application's Configuration method. e.g.:
 
using Microsoft.AspNet.SignalR;
using Owin;
 
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

Enabling cross-domain requests
---------------------------------------
To enable CORS requests, Install-Package Microsoft.Owin.Cors and change the startup class to look like the following:

using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;
 
namespace MyWebApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/signalr", map =>
            {
                // Setup the cors middleware to run before SignalR.
                // By default this will allow all origins. You can 
                // configure the set of origins and/or http verbs by
                // providing a cors options with a different policy.
                map.UseCors(CorsOptions.AllowAll);
                
                var hubConfiguration = new HubConfiguration 
                {
                    // You can enable JSONP by uncommenting line below.
                    // JSONP requests are insecure but some older browsers (and some
                    // versions of IE) require JSONP to work cross domain
                    // EnableJSONP = true
                };
                
                // Run the SignalR pipeline. We're not using MapSignalR
                // since this branch is already runs under the "/signalr"
                // path.
                map.RunSignalR(hubConfiguration);
            });
        }
    }
}


 
Starting the Web server
--------------------------------
To start the web server, call WebApp.Start<Startup>(endpoint). You should now be able to navigate to endpoint/signalr/hubs in your browser.
 
using System;
using Microsoft.Owin.Hosting;
 
namespace MyWebApplication
{
    public class Program
    {
        static void Main(string[] args)
        {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 or http://+:8080 to bind to all addresses. 
            // See http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx 
            // for more information.
            
            using (WebApp.Start<Startup>("http://localhost:8080/"))
            {
                Console.WriteLine("Server running at http://localhost:8080/");
                Console.ReadLine();
            }
        }
    }
}

