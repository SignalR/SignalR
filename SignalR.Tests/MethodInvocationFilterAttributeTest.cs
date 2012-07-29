using System;
using SignalR.Hubs;
using SignalR.Hubs.Attributes;
using Xunit;

namespace SignalR.Tests
{
    public class MethodInvocationFilterAttributeTest
    {
        [Fact]
        public void ResolveActionWithMultipleAttributesFindsAllFilters()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(AttributeTestHub), Name = "AttributeTestHub" }, "FooWithMultipleAttributes", out actionInfo);
            Assert.NotNull(actionInfo);
            Assert.Equal(2, actionInfo.InvocationFilters.Count);
        }

        [Fact]
        public void ResolveAttributesWhenGlobalAttributeIsAssigned()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(GlobalAttributeTestHub), Name = "GlobalAttributeTestHub" }, "Foo", out actionInfo);
            Assert.NotNull(actionInfo);
            Assert.Equal(1, actionInfo.InvocationFilters.Count);
        }

        [Fact]
        public void ResolveAttributesWhenGlobalAndLocalAttributeAssigned()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(GlobalAttributeTestHub), Name = "GlobalAttributeTestHub" }, "FooBar", out actionInfo);
            Assert.NotNull(actionInfo);
            Assert.Equal(2, actionInfo.InvocationFilters.Count);            
        }
        
        private class AttributeTestHub : Hub
        {
            [DummyFilterBeforeOnly]
            [DummyFilterAfterOnly]
            public void FooWithMultipleAttributes()
            {

            }
        }

        [DummyFilter]
        private class GlobalAttributeTestHub : Hub
        {
            public string Foo()
            {
                return "Hello";
            }
            
            public string Bar()
            {
                return "Hello";
            }

            [DummyFilterAfterOnly]
            public string FooBar()
            {
                return "World";
            }
        }

        private class DummyFilterAttribute : MethodInvocationFilterAttribute
        {
            public override void OnMethodInvoked(IHub hub, ref object result)
            {
                base.OnMethodInvoked(hub, ref result);
            }

            public override void OnMethodInvoking(IHub hub)
            {
                throw new NotImplementedException();
            }
        }

        private class DummyFilterBeforeOnlyAttribute : MethodInvocationFilterAttribute
        {
            public override void OnMethodInvoking(IHub hub)
            {
                throw new NotImplementedException();
            }
        }

        private class DummyFilterAfterOnlyAttribute : MethodInvocationFilterAttribute
        {
            public override void OnMethodInvoked(IHub hub, ref object result)
            {
                throw new NotImplementedException();
            }
        }
    }
}