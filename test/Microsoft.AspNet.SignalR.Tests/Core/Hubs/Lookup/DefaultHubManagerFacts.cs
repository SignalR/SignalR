// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class DefaultHubManagerFacts
    {
        [Fact]
        public void GetValidHub()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptor = hubManager.GetHub("CoreTestHub");

            Assert.NotNull(hubDescriptor);
            Assert.False(hubDescriptor.NameSpecified);
        }

        [Fact]
        public void GetNonExistantHub()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptor = hubManager.GetHub("__ELLO__");

            Assert.Null(hubDescriptor);
        }

        [Fact]
        public void GetInvalidHubThrows()
        {
            var hub = new HubDescriptor()
            {
                Name = "this.is.not.valid"
            };
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IHubDescriptorProvider), () => new TestHubDescriptorProvider(hub));
            var hubManager = new DefaultHubManager(resolver);

            var ex = Assert.Throws<InvalidOperationException>(() => hubManager.GetHub(hub.Name));
            Assert.Equal(string.Format(Resources.Error_HubNameIsInvalid, hub.Name), ex.Message);
        }

        [Fact]
        public void GetValidHubsWithoutPredicate()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptors = hubManager.GetHubs(predicate: null);

            Assert.NotNull(hubDescriptors);

            HubDescriptor coreTestHub = hubDescriptors.First(descriptor => descriptor.Name == "CoreTestHub");

            Assert.NotNull(coreTestHub);
        }

        [Fact]
        public void GetValidHubsWithValidPredicate()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptors = hubManager.GetHubs(descriptor => descriptor.Name == "CoreTestHub");

            Assert.NotNull(hubDescriptors);
            Assert.Equal(hubDescriptors.First().Name, "CoreTestHub");
        }

        [Fact]
        public void GetValidHubsWithInvalidPredicate()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptors = hubManager.GetHubs(descriptor => descriptor.Name == "CoreTestHub_INVALIDHUB_____");

            // Still have an ienumerable sequence
            Assert.NotNull(hubDescriptors);
            // But there's nothing in the ienumerable
            Assert.Empty(hubDescriptors);
        }

        [Fact]
        public void GetHubsWithInvalidHubThrows()
        {
            var hub = new HubDescriptor()
            {
                Name = "this.is.not.valid"
            };
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IHubDescriptorProvider), () => new TestHubDescriptorProvider(hub));
            var hubManager = new DefaultHubManager(resolver);

            var ex = Assert.Throws<InvalidOperationException>(() => hubManager.GetHubs());
            Assert.Equal(string.Format(Resources.Error_HubNameIsInvalid, hub.Name), ex.Message);
        }

        [Fact]
        public void GetHubsDoesNotThrowIfPredicateSkipsInvalidHub()
        {
            var invalidHub = new HubDescriptor()
            {
                Name = "this.is.not.valid"
            };
            var validHub = new HubDescriptor()
            {
                Name = "thisisvalid"
            };
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IHubDescriptorProvider), () => new TestHubDescriptorProvider(invalidHub, validHub));
            var hubManager = new DefaultHubManager(resolver);

            Assert.Collection(
                hubManager.GetHubs(h => !h.Name.Contains(".")),
                h => Assert.Equal(validHub.Name, h.Name));
        }

        [Fact]
        public void GetValidHubMethod()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var methodDescriptor = hubManager.GetHubMethod("CoreTestHubWithMethod", "AddNumbers", new IJsonValue[] { null, null });

            Assert.NotNull(methodDescriptor);
            Assert.Equal(methodDescriptor.Name, "AddNumbers");
            Assert.Equal(methodDescriptor.ReturnType, typeof(int));

            IList<ParameterDescriptor> parameters = methodDescriptor.Parameters;
            Assert.Equal(parameters.Count, 2);
            Assert.Equal(parameters[0].Name, "first");
            Assert.Equal(parameters[0].ParameterType, typeof(int));
            Assert.Equal(parameters[1].Name, "second");
            Assert.Equal(parameters[1].ParameterType, typeof(int));
        }

        [Fact]
        public void GetHubMethodWithIncorrectParameters()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            // The AddNumbers method has 2 parameters, so should not find the method
            var methodDescriptor = hubManager.GetHubMethod("CoreTestHubWithMethod", "AddNumbers", null);

            Assert.Null(methodDescriptor);
        }

        [Fact]
        public void GetHubMethodFromNonExistantHub()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            // There is no ________________CoreTestHubWithMethod________________ name
            var methodDescriptor = hubManager.GetHubMethod("________________CoreTestHubWithMethod________________", "AddNumbers", new IJsonValue[] { null, null });

            Assert.Null(methodDescriptor);
        }

        [Fact]
        public void GetHubMethodFromValidHubWhenInvalidHubIsRegisteredDoesNotThrow()
        {
            var validHub = new HubDescriptor()
            {
                Name = "Valid"
            };
            var invalidHub = new HubDescriptor()
            {
                Name = "this.is.not.valid"
            };
            var method = new MethodDescriptor()
            {
                Name = "Method",
                Parameters = new List<ParameterDescriptor>()
            };
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IHubDescriptorProvider), () => new TestHubDescriptorProvider(validHub, invalidHub));
            resolver.Register(typeof(IMethodDescriptorProvider), () => new TestMethodDescriptorProvider(validHub.Name, method));
            var hubManager = new DefaultHubManager(resolver);

            Assert.Same(
                method,
                hubManager.GetHubMethod(validHub.Name, "Method", Array.Empty<IJsonValue>()));
        }

        [Fact]
        public void GetHubMethodFromInvalidHubThrows()
        {
            var hub = new HubDescriptor()
            {
                Name = "this.is.not.valid"
            };
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IHubDescriptorProvider), () => new TestHubDescriptorProvider(hub));
            var hubManager = new DefaultHubManager(resolver);

            var ex = Assert.Throws<InvalidOperationException>(() => hubManager.GetHubMethod(hub.Name, "Method", new IJsonValue[] { null, null }));
            Assert.Equal(string.Format(Resources.Error_HubNameIsInvalid, hub.Name), ex.Message);
        }

        [Fact]
        public void GetValidHubMethodsWithoutPredicate()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var methodDescriptors = hubManager.GetHubMethods("CoreTestHubWithMethod", predicate: null);

            Assert.NotNull(methodDescriptors);

            MethodDescriptor coreTestHubMethod = methodDescriptors.First(descriptor => descriptor.Name == "AddNumbers");

            Assert.NotNull(coreTestHubMethod);
        }

        [Fact]
        public void GetValidHubMethodsWithPredicate()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var methodDescriptors = hubManager.GetHubMethods("CoreTestHubWithMethod", descriptor => descriptor.Name == "AddNumbers");

            Assert.NotNull(methodDescriptors);
            Assert.Equal(methodDescriptors.First().Name, "AddNumbers");
        }

        [Fact]
        public void GetHubMethodsWithInvalidHubThrows()
        {
            var hub = new HubDescriptor()
            {
                Name = "this.is.not.valid"
            };
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IHubDescriptorProvider), () => new TestHubDescriptorProvider(hub));
            var hubManager = new DefaultHubManager(resolver);

            var ex = Assert.Throws<InvalidOperationException>(() => hubManager.GetHubMethods(hub.Name, predicate: null));
            Assert.Equal(string.Format(Resources.Error_HubNameIsInvalid, hub.Name), ex.Message);
        }

        [Fact]
        public void GetValidHubMethodsWithInvalidPredicate()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var methodDescriptors = hubManager.GetHubMethods("CoreTestHubWithMethod", descriptor => descriptor.Name == "______AddNumbers______");

            // Still have an ienumerable sequence
            Assert.NotNull(methodDescriptors);
            // But there's nothing in the ienumerable
            Assert.Empty(methodDescriptors);
        }

        [Fact]
        public void ResolveValidHub()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptor = hubManager.ResolveHub("CoreTestHub");

            Assert.NotNull(hubDescriptor);
        }

        [Fact]
        public void ResolveNonExistantHub()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptor = hubManager.ResolveHub("____CoreTestHub____");

            Assert.Null(hubDescriptor);
        }

        [Fact]
        public void ResolveInvalidHubThrows()
        {
            var hub = new HubDescriptor()
            {
                Name = "this.is.not.valid"
            };
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IHubDescriptorProvider), () => new TestHubDescriptorProvider(hub));
            var hubManager = new DefaultHubManager(resolver);

            var ex = Assert.Throws<InvalidOperationException>(() => hubManager.ResolveHub(hub.Name));
            Assert.Equal(string.Format(Resources.Error_HubNameIsInvalid, hub.Name), ex.Message);
        }

        [Fact]
        public void ResolveHubsIsNotEmpty()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptor = hubManager.ResolveHubs();

            Assert.NotNull(hubDescriptor);
            Assert.NotEmpty(hubDescriptor);
        }
    }
}
