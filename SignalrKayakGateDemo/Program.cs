using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Gate.Hosts.Kayak;
using SignalR;
using SignalR.Abstractions;

namespace SignalrKayakGateDemo
{
    public class Program
    {
        // plain-ol' entry point.
        public static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(TaskScheduler_UnobservedTaskException);
            var ep = new IPEndPoint(IPAddress.Any, 5500);
            Console.WriteLine("Listening on " + ep);

            KayakGate.Start(new SchedulerDelegate(), ep, Startup.Configuration);
        }

        static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.GetBaseException());
            e.SetObserved();
        }
    }
}
