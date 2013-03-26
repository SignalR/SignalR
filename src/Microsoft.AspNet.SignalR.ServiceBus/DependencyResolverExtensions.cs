// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.ServiceBus;

namespace Microsoft.AspNet.SignalR
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
            return UseWindowsAzureServiceBus(resolver, connectionString, instanceCount, topicCount: 1, messageDefaultTtl: TimeSpan.MaxValue);
        }

        /// <summary>
        /// Use the Windows Azure Service Bus backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The service bus connection string.</param>
        /// <param name="instanceCount">The number of role instances in the deployment.</param>
        /// <param name="topicCount">The number of topics to use.</param>
        /// <param name="messageDefaultTtl">The time to live of the message.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseWindowsAzureServiceBus(this IDependencyResolver resolver, string connectionString, int instanceCount, int topicCount, TimeSpan messageDefaultTtl)
        {
            AzureRoleInfo azureRole = null;

            try
            {
                azureRole = GetRoleInfo();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_UnableToResolveIncaseIndexOfRole), ex);
            }

            return UseServiceBus(resolver, connectionString, topicCount, instanceCount, azureRole.Index, azureRole.Name, messageDefaultTtl);
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
        /// <param name="messageDefaultTtl">The time to live of the message.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseServiceBus(this IDependencyResolver resolver, string connectionString, int topicCount, int instanceCount, int instanceIndex, string topicPrefix, TimeSpan messageDefaultTtl)
        {
            var bus = new Lazy<ServiceBusMessageBus>(() => new ServiceBusMessageBus(connectionString, topicCount, instanceCount, instanceIndex, topicPrefix, resolver, messageDefaultTtl));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.LastIndexOf(System.String)", Justification = "Comparing non alpha numeric characters."), MethodImpl(MethodImplOptions.NoInlining)]
        private static AzureRoleInfo GetRoleInfo()
        {
            var roleInstance = Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.CurrentRoleInstance;
            var roleInstanceId = roleInstance.Id;
            var li1 = roleInstanceId.LastIndexOf(".");
            var li2 = roleInstanceId.LastIndexOf("_");
            var roleInstanceNo = roleInstanceId.Substring(Math.Max(li1, li2) + 1);

            return new AzureRoleInfo
            {
                Index = Int32.Parse(roleInstanceNo, CultureInfo.CurrentCulture),
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
