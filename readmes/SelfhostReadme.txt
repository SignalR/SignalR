Please see http://go.microsoft.com/fwlink/?LinkId=272764 for more information on using SignalR.
 
Mapping the Hubs connection
----------------------------------------
SignalR Hubs will not work without a Hub route being configured. To register the default Hubs route, create a class called Startup 
with the signature below and call app.MapHubs() in  your application's Configuration method. e.g.:
 
using Microsoft.AspNet.SignalR;
using Owin;
 
namespace MyWebApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapHubs();
        }
    }
}
 
Starting the Web server
--------------------------------
To start the web server, call WebApplication.Start<Startup>(endpoint). You should now be able to navigate to endpoint/signalr/hubs in your browser.
 
using System;
using Microsoft.Owin.Hosting;
 
namespace MyWebApplication
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (WebApplication.Start<Startup>("http://localhost:8080/"))
            {
                Console.WriteLine("Server running at http://localhost:8080/");
                Console.ReadLine();
            }
        }
    }
}

