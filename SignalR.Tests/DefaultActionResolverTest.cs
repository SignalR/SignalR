using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
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
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "AddToGroup", out actionInfo1, new[] { JTokenify("admin") });
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "RemoveFromGroup", out actionInfo2, new[] { JTokenify("admin") });

            Assert.Null(actionInfo1);
            Assert.Null(actionInfo2);
        }

        [Fact]
        public void ResolveActionOnDerivedHubFindsMethodOnBasedType()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestDerivedHub), Name = "TestHub" }, "Foo", out actionInfo, new IParameterValue[] { });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(0, actionInfo.Parameters.Count);
        }

        [Fact]
        public void ResolveActionExcludesPropertiesOnDeclaredType()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "get_Value", out actionInfo, new IParameterValue[] { });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionExcludesPropetiesOnBaseTypes()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "get_Clients", out actionInfo, new IParameterValue[] { });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionLocatesPublicMethodsOnHub()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "Foo", out actionInfo, new IParameterValue[] { });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(0, actionInfo.Parameters.Count);
        }

        [Fact]
        public void ResolveActionReturnsNullIfMethodAmbiguous()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "Bar", out actionInfo, new[] { JTokenify(1) });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionPicksMethodWithMatchingArguments()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "Foo", out actionInfo, new[] { JTokenify(1) });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(1, actionInfo.Parameters.Count);
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
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithComplex", out actionInfo, new IParameterValue[] { new JTokenParameterValue(arg) });

            Assert.NotNull(actionInfo);
            var complex = binder.ResolveMethodParameters(actionInfo, new JTokenParameterValue(arg))[0] as Complex;
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
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithArray", out actionInfo, new IParameterValue[] { new JTokenParameterValue(arg) });

            Assert.NotNull(actionInfo);
            var args = binder.ResolveMethodParameters(actionInfo, new JTokenParameterValue(arg))[0] as int[];
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
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithArrayOfComplete", out actionInfo, new IParameterValue[] { new JTokenParameterValue(new JArray(new object[] { arg })) });

            Assert.NotNull(actionInfo);
            var complexArray = binder.ResolveMethodParameters(actionInfo, new JTokenParameterValue(new JArray(new object[] { arg })))[0] as Complex[];
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

            var arg = JTokenify(new Guid("1d6a1d30-599f-4495-ace7-303fd87204bb"));

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithGuid", out actionInfo, new IParameterValue[] { arg });

            Assert.NotNull(actionInfo);
            var arg0 = (Guid)binder.ResolveMethodParameters(actionInfo, arg)[0];
            Assert.Equal(new Guid("1d6a1d30-599f-4495-ace7-303fd87204bb"), arg0);
        }

        [Fact]
        public void ResolveActionBindsByteArray()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = JTokenify(Encoding.UTF8.GetBytes("Hello World!"));

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithByteArray", out actionInfo, new IParameterValue[] { arg });

            Assert.NotNull(actionInfo);
            var arg0 = (byte[])binder.ResolveMethodParameters(actionInfo, arg)[0];
            Assert.Equal("Hello World!", Encoding.UTF8.GetString(arg0));
        }

        [Fact]
        public void ResolveActionBindsArrayOfByteArray()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = JTokenify(new[] { Encoding.UTF8.GetBytes("Hello World!") });

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodListOfByteArray", out actionInfo, new IParameterValue[] { arg });

            Assert.NotNull(actionInfo);
            var arg0 = (List<byte[]>)binder.ResolveMethodParameters(actionInfo, arg)[0];
            Assert.Equal("Hello World!", Encoding.UTF8.GetString(arg0[0]));
        }

        [Fact]
        public void ResolveActionBindsNullables()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg1 = JTokenify(null);
            var arg2 = JTokenify(null);

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { Type = typeof(TestHub), Name = "TestHub" }, "MethodWithNullables", out actionInfo, new IParameterValue[] { arg1, arg2 });

            Assert.NotNull(actionInfo);
            var args = binder.ResolveMethodParameters(actionInfo, arg1, arg2);
            Assert.Null(args[0]);
            Assert.Null(args[1]);
        }

        private IParameterValue JTokenify(object value)
        {
            return new JTokenParameterValue(JToken.Parse(JsonConvert.SerializeObject(value)));
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

            public void MethodWithByteArray(byte[] data)
            {

            }

            public void MethodListOfByteArray(List<byte[]> datas)
            {

            }

            public void MethodWithNullables(int? x, string y)
            {

            }

            public void MethodWithNonNullable(int x)
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
