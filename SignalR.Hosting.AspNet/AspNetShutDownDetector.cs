using System;
using System.Diagnostics;
using System.Web.Hosting;

namespace SignalR.Hosting.AspNet
{
    internal class AspNetShutDownDetector : IRegisteredObject
    {
        private readonly Action _onShutdown;

        public AspNetShutDownDetector(Action onShutdown)
        {
            _onShutdown = onShutdown;
        }

        public void Initialize()
        {
            try
            {
                HostingEnvironment.RegisterObject(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void Stop(bool immediate)
        {
            try
            {
                _onShutdown();
            }
            catch
            {
                // Swallow the exception as Stop should never throw
                // TODO: Log exceptions
            }
            finally
            {
                HostingEnvironment.UnregisterObject(this);
            }
        }
    }
}
