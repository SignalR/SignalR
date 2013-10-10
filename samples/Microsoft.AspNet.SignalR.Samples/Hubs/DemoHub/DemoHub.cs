using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub
{
    [HubName("demo")]
    public class DemoHub : Hub
    {
        private static readonly TaskCompletionSource<object> _neverEndingTcs = new TaskCompletionSource<object>();

        public Task<int> GetValue()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                return 10;
            });
        }

        public void SendToUser(string userId)
        {
            Clients.User(userId).invoke();
        }

        public void AddToGroups()
        {
            Groups.Add(Context.ConnectionId, "foo");
            Groups.Add(Context.ConnectionId, "bar");
            Clients.Caller.groupAdded();
        }

        public void DoSomethingAndCallError()
        {
            Clients.Caller.errorInCallback();
        }

        public Task DynamicTask()
        {
            return Clients.All.signal(Guid.NewGuid());
        }

        public async Task PlainTask()
        {
            await Task.Delay(500);
        }

        public async Task<int> GenericTaskWithContinueWith()
        {
            return await Task.Run(() => 2 + 2).ContinueWith(task => task.Result);
        }

        public async Task TaskWithException()
        {
            await Task.Factory.StartNew(() =>
            {
                throw new Exception();
            });
        }

        public async Task<int> GenericTaskWithException()
        {
            return await Task<int>.Factory.StartNew(() =>
            {
                throw new Exception();
            });
        }

        public void SynchronousException()
        {
            throw new Exception();
        }

        public void HubException()
        {
            throw new HubException("message", "errorData");
        }

        public void HubExceptionWithoutErrorData()
        {
            throw new HubException("message");
        }

        public Task CancelledTask()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        public Task<int> CancelledGenericTask()
        {
            var tcs = new TaskCompletionSource<int>();
            return Task.Factory.StartNew(() =>
            {
                tcs.SetCanceled();
                return tcs.Task;
            }).Unwrap();
        }

        public Task NeverEndingTask()
        {
            return _neverEndingTcs.Task;
        }

        public void SimpleArray(int[] nums)
        {
            foreach (var n in nums)
            {
            }
        }

        public string ReadStateValue()
        {
            return Clients.Caller.name;
        }

        public string SetStateValue(string value)
        {
            Clients.Caller.Company = value;

            return Clients.Caller.Company;
        }

#if !SELFHOST
        public string GetHttpContextHandler()
        {
            object value;
            if (Context.Request.Environment.TryGetValue(typeof(HttpContextBase).FullName, out value))
            {
                var context = value as HttpContextBase;
                if (context != null && context.Handler != null)
                {
                    return context.Handler.GetType().Name;
                }
            }

            return null;
        }
#endif

        public object ReadAnyState()
        {
            Clients.Caller.state2 = Clients.Caller.state;
            Clients.Caller.addy = Clients.Caller.state.Address;

            string name = Clients.Caller.state["Name"];
            string street = Clients.Caller.state["Address"]["Street"];

            string dname = Clients.Caller.state.Name;
            string dstreet = Clients.Caller.state.Address.Street;

            if (!name.Equals(dname))
            {
                throw new InvalidOperationException("Fail");
            }

            if (!street.Equals(dstreet))
            {
                throw new InvalidOperationException("Fail");
            }

            return Clients.Caller.state;
        }

        public void ComplexArray(Person[] people)
        {

        }

        public Person ComplexType(Person p)
        {
            Clients.Caller.person = p;
            return p;
        }

        public int PassingDynamicComplex(dynamic p)
        {
            return p.Age;
        }

        public void MultipleCalls()
        {
            for (int i = 0; i < 10; i++)
            {
                Clients.Caller.index = i + 1;
                Clients.Caller.invoke(i);
                Thread.Sleep(1000);
            }
        }

        public async Task ReportProgress(string jobName, IProgress<int> progress)
        {
            for (int i = 0; i <= 100; i += 10)
            {
                await Task.Delay(250);
                progress.Report(i);
            }
        }

        public void Overload()
        {

        }

        public int Overload(int n)
        {
            return n;
        }

        public string InlineScriptTag()
        {
            return "WAITING for Script Tag to replace this.<script>$(\"#inlineScriptTag\").html('Success! Replaced by inline Script Tag');</script>";
        }

        public void UnsupportedOverload(string x)
        {

        }

        public void UnsupportedOverload(int x)
        {

        }

        public void TestGuid()
        {
            Clients.Caller.TestGuid(new Guid());
        }

        public void DynamicInvoke(string method)
        {
            IClientProxy proxy = Clients.Caller;
            proxy.Invoke(method);
        }

        public void MispelledClientMethod()
        {
            Clients.Caller.clientMethd();
        }

        public string ReturnLargePayload()
        {
            return new string('a', 64 * 1024);
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