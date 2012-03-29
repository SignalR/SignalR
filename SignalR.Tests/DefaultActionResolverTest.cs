using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using SignalR.Hubs;
using Xunit;

namespace SignalR.Tests
{
    public class DefaultActionResolverTest
    {
        [Fact]
        public void ResolveActionExcludeHubMethods()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo1;
            MethodDescriptor actionInfo2;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "AddToGroup", out actionInfo1, new object[] { "admin" });
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "RemoveFromGroup", out actionInfo2, new object[] { "admin" });

            Assert.Null(actionInfo1);
            Assert.Null(actionInfo2);
        }

        [Fact]
        public void ResolveActionOnDerivedHubFindsMethodOnBasedType()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestDerivedHub), Name = "TestHub" }, "Foo", out actionInfo, new object[] { });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(0, actionInfo.Parameters.Count());
        }

        [Fact]
        public void ResolveActionExcludesPropertiesOnDeclaredType()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "get_Value", out actionInfo, new object[] { });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionExcludesPropetiesOnBaseTypes()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "get_Clients", out actionInfo, new object[] { });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionLocatesPublicMethodsOnHub()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "Foo", out actionInfo, new object[] { });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(0, actionInfo.Parameters.Count());
        }

        [Fact]
        public void ResolveActionReturnsNullIfMethodAmbiguous()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "Bar", out actionInfo, new object[] { 1 });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionPicksMethodWithMatchingArguments()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "Foo", out actionInfo, new object[] { 1 });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(1, actionInfo.Parameters.Count());
        }

        [Fact]
        public void ResolveActionBindsComplexArguments()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = new JObject(new JProperty("Age", 1),
                                  new JProperty("Address",
                                      new JObject(
                                          new JProperty("Street", "The street"),
                                          new JProperty("Zip", "34567"))));


            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithComplex", out actionInfo, new object[] { arg });

            Assert.NotNull(actionInfo);
            var complex = binder.ResolveMethodParameters(actionInfo, arg)[0] as Complex;
            Assert.NotNull(complex);
            Assert.Equal(1, complex.Age);
            Assert.NotNull(complex.Address);
            Assert.Equal("The street", complex.Address.Street);
            Assert.Equal(34567, complex.Address.Zip);
        }

        [Fact]
        public void ResolveActionBindsSimpleArrayArgument()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = new JArray(new[] { 1, 2, 3 });

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithArray", out actionInfo, new object[] { arg });

            Assert.NotNull(actionInfo);
            var args = binder.ResolveMethodParameters(actionInfo, arg)[0] as int[];
            Assert.Equal(1, args[0]);
            Assert.Equal(2, args[1]);
            Assert.Equal(3, args[2]);
        }

        [Fact]
        public void ResolveActionBindsComplexArrayArgument()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = new JObject(new JProperty("Age", 1),
                                  new JProperty("Address",
                                      new JObject(
                                          new JProperty("Street", "The street"),
                                          new JProperty("Zip", "34567"))));


            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithArrayOfComplete", out actionInfo, new object[] { new JArray(new object[] { arg }) });

            Assert.NotNull(actionInfo);
            var complexArray = binder.ResolveMethodParameters(actionInfo, new JArray(new object[] { arg }))[0] as Complex[];
            Assert.Equal(1, complexArray.Length);
            var complex = complexArray[0];
            Assert.NotNull(complex);
            Assert.Equal(1, complex.Age);
            Assert.NotNull(complex.Address);
            Assert.Equal("The street", complex.Address.Street);
            Assert.Equal(34567, complex.Address.Zip);
        }

        [Fact]
        public void ResolveActionBindsGuid()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = "1d6a1d30-599f-4495-ace7-303fd87204bb";

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithGuid", out actionInfo, new object[] { arg });

            Assert.NotNull(actionInfo);
            var arg0 = (Guid)binder.ResolveMethodParameters(actionInfo, arg)[0];
            Assert.Equal(new Guid(arg), arg0);
        }

        private class TestDerivedHub : TestHub
        {
            public void FooDerived()
            {
                Foo();
            }
        }

        private class TestHub : Hub
        {
            public int Value { get; set; }

            public void Foo()
            {
            }

            public void Foo(int x)
            {

            }

            public void Bar(double d)
            {

            }

            public void Bar(int x)
            {

            }

            public void MethodWithArray(int[] values)
            {

            }

            public void MethodWithArrayOfComplete(Complex[] complexes)
            {

            }

            public void MethodWithComplex(Complex complex)
            {

            }

            public void MethodWithGuid(Guid guid)
            {

            }
        }

        public class Complex
        {
            public int Age { get; set; }
            public Address Address { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public int Zip { get; set; }
        }
    }
}
