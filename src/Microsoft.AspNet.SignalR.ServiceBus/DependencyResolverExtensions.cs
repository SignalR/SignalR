// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use the Windows Azure Service Bus backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The service bus connection string.</param>
        /// <param name="instanceCount">The number of role instances in the deployment.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseWindowsAzureServiceBus(this IDependencyResolver resolver, string connectionString, int instanceCount)
        {
            return UseWindowsAzureServiceBus(resolver, connectionString, instanceCount, topicCount: 1);
        }

        /// <summary>
        /// Use the Windows Azure Service Bus backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The service bus connection string.</param>
        /// <param name="instanceCount">The number of role instances in the deployment.</param>
        /// <param name="topicCount">The number of topics to use.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseWindowsAzureServiceBus(this IDependencyResolver resolver, string connectionString, int instanceCount, int topicCount)
        {
            AzureRoleInfo azureRole = null;

            try
            {
                azureRole = GetRoleInfo();
            }
            catch (TypeLoadException ex)
            {
                throw new InvalidOperationException("Unable to resolve the instance index of this role. Make sure Microsoft.WindowsAzure.ServiceRuntime.dll is deployed with your application.", ex);
            }

            return UseServiceBus(resolver, connectionString, topicCount, instanceCount, azureRole.Index, azureRole.Name);
        }

        /// <summary>
        /// Use the Windows Azure Service Bus backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The service bus connection string.</param>
        /// <param name="topicCount">The number of topics to use.</param>
        /// <param name="instanceCount">The number of role instances in the deployment.</param>
        /// <param name="instanceIndex">The zero-based index of this role instance in the deployment.</param>
        /// <param name="topicPrefix">The topic prefix.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, string connectionString, int topicCount, int instanceCount, int instanceIndex, string topicPrefix)
        {
            var bus = new Lazy<ServiceBusMessageBus>(() => new ServiceBusMessageBus(connectionString, topicCount, instanceCount, instanceIndex, topicPrefix, resolver));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static AzureRoleInfo GetRoleInfo()
        {
            var roleInstance = Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.CurrentRoleInstance;
            var roleInstanceId = roleInstance.Id;
            var li1 = roleInstanceId.LastIndexOf(".");
            var li2 = roleInstanceId.LastIndexOf("_");
            var roleInstanceNo = roleInstanceId.Substring(Math.Max(li1, li2) + 1);

            return new AzureRoleInfo
            {
                Index = Int32.Parse(roleInstanceNo),
                Name = roleInstance.Role.Name
            };
        }

        private class AzureRoleInfo
        {
            public string Name { get; set; }
            public int Index { get; set; }
        }
    }
}
