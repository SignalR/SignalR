using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hubs;

namespace SignalR.Samples.Hubs.DemoHub
{
    [HubName("demo")]
    public class DemoHub : Hub
    {
        public Task<int> GetValue()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                return 10;
            });
        }

        public void AddToGroups()
        {
            AddToGroup("foo");
            AddToGroup("bar");
            Caller.groupAdded();
        }

        public void DoSomethingAndCallError()
        {
            Clients.errorInCallback();
        }

        public Task DynamicTask()
        {
            return Clients.signal(Guid.NewGuid());
        }

        public Task PlainTask()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(500);
            });
        }

        public Task GenericTaskTypedAsPlain()
        {
            return Task.Factory.StartNew(() =>
            {
                return 2 + 2;
            });
        }

        public Task TaskWithException()
        {
            return Task.Factory.StartNew(() =>
            {
                throw new Exception();
            });
        }

        public void SimpleArray(int[] nums)
        {
            foreach (var n in nums)
            {
            }
        }

        public void ComplexArray(Person[] people)
        {

        }

        public Person ComplexType(Person p)
        {
            Caller.person = p;
            return p;
        }

        public void MultipleCalls()
        {
            for (int i = 0; i < 10; i++)
            {
                Caller.index = i + 1;
                Caller.invoke(i);
                Thread.Sleep(1000);
            }
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public Address Address { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public string Zip { get; set; }
        }
    }
}