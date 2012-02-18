﻿using System.Web;
using System.Web.Script.Serialization;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using SignalR.Infrastructure;
using SignalR.ScaleOut;
using SignalR.Web;

[assembly: PreApplicationStartMethod(typeof(PreApplicationStart), "Start")]

namespace SignalR.ScaleOut
{
    public static class PreApplicationStart
    {

        public static void Start()
        {
            // DependencyResolver.Default.Register(typeof(IPeerUrlSource), () => new ConfigPeerUrlSource());
            // DependencyResolver.Default.Register(typeof(IJsonSerializer), () => serializer);

            // DynamicModuleUtility.RegisterModule(typeof(SignalReceiverModule));
            // DynamicModuleUtility.RegisterModule(typeof(MessageReceiverModule));
        }

    }
}