﻿using System;
using System.Web.Hosting;

namespace SignalR.Hosting.AspNet
{
    internal class AspNetShutDownDetector : IRegisteredObject
    {
        private readonly Action _onShutdown;

        public AspNetShutDownDetector(Action onShutdown)
        {
            _onShutdown = onShutdown;
            HostingEnvironment.RegisterObject(this);
        }

        public void Stop(bool immediate)
        {
            try
            {
                if (!immediate)
                {
                    _onShutdown();
                }
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
