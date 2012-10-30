// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Server.Handlers;

namespace Owin
{
    public static class OwinSignalRExtensions
    {
        public static IAppBuilder MapHubs(this IAppBuilder builder)
        {
            return builder.UseType<HubDispatcherHandler>();
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path)
        {
            return builder.UseType<HubDispatcherHandler>(path);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, IDependencyResolver resolver)
        {
            return builder.UseType<HubDispatcherHandler>(resolver);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path, IDependencyResolver resolver)
        {
            return builder.UseType<HubDispatcherHandler>(path, resolver);
        }

        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url)
        {
            return builder.UseType<PersistentConnectionHandler>(url, typeof (T));
        }

        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url, IDependencyResolver resolver)
        {
            return builder.UseType<PersistentConnectionHandler>(url, typeof (T), resolver);
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType)
        {
            return builder.UseType<PersistentConnectionHandler>(url, connectionType);
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType, IDependencyResolver resolver)
        {
            return builder.UseType<PersistentConnectionHandler>(url, connectionType, resolver);
        }

        private static IAppBuilder UseType<T>(this IAppBuilder builder, params object[] args)
        {
            return builder.Use(typeof (T), args);
        }
    }
}
