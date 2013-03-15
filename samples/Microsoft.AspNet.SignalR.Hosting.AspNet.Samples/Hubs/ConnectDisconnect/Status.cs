using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.ConnectDisconnect
{
    [HubName("StatusHub")]
    public class Status : Hub
    {
        public override Task OnDisconnected()
        {
            return Clients.All.leave(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task OnConnected()
        {
            return Clients.All.joined(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task OnReconnected()
        {
            return Clients.All.rejoined(Context.ConnectionId, DateTime.Now.ToString());
        }

        public void Ping()
        {
            Clients.Caller.pong();
        }

        public Task SendMessage(string msg)
        {
            // Clients.All.addMessage(msg).wait();
            Person p = new Person(); 
            p.Name = "John"; p.Age=12;
            return Clients.All.addPerson(p);
        }

        class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
    }
}