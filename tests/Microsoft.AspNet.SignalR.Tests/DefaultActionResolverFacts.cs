using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class DefaultActionResolverFacts
    {
        [Fact]
        public void ResolveActionExcludesHubMethods()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo1;
            MethodDescriptor actionInfo2;
            MethodDescriptor actionInfo3;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(HubWithOverrides), Name = "TestHub" }, "OnDisconnected", out actionInfo1, new IJsonValue[] { });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(HubWithOverrides), Name = "TestHub" }, "OnReconnected", out actionInfo2, new IJsonValue[] { });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(HubWithOverrides), Name = "TestHub" }, "OnConnected", out actionInfo3, new IJsonValue[] { });

            Assert.Null(actionInfo1);
            Assert.Null(actionInfo2);
            Assert.Null(actionInfo3);
        }

        [Fact]
        public void ResolveActionExcludesIHubMethods()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo1;
            MethodDescriptor actionInfo2;
            MethodDescriptor actionInfo3;
            MethodDescriptor actionInfo4;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(HubWithOverrides2), Name = "TestHub" }, "OnDisconnected", out actionInfo1, new IJsonValue[] { });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(HubWithOverrides2), Name = "TestHub" }, "OnReconnected", out actionInfo2, new IJsonValue[] { });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(HubWithOverrides2), Name = "TestHub" }, "OnConnected", out actionInfo3, new IJsonValue[] { });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(HubWithOverrides2), Name = "TestHub" }, "Dispose", out actionInfo4, new IJsonValue[] { });

            Assert.Null(actionInfo1);
            Assert.Null(actionInfo2);
            Assert.Null(actionInfo3);
            Assert.Null(actionInfo4);
        }

        [Fact]
        public void ResolveActionExcludesObjectMethods()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo1;
            MethodDescriptor actionInfo2;
            MethodDescriptor actionInfo3;
            MethodDescriptor actionInfo4;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(MyHubWithObjectMethods), Name = "TestHub" }, "GetHashCode", out actionInfo1, new IJsonValue[] { });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(MyHubWithObjectMethods), Name = "TestHub" }, "Equals", out actionInfo2, new IJsonValue[] { JTokenify("test") });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(MyHubWithObjectMethods), Name = "TestHub" }, "ToString", out actionInfo3, new IJsonValue[] { });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(MyHubWithObjectMethods), Name = "TestHub" }, "Dispose", out actionInfo4, new IJsonValue[] { JTokenify(false) });

            Assert.Null(actionInfo1);
            Assert.Null(actionInfo2);
            Assert.Null(actionInfo3);
            Assert.Null(actionInfo4);
        }

        [Fact]
        public void ResolveActionExcludesEvents()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo1;
            MethodDescriptor actionInfo2;

            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(MyHubWithEvents), Name = "TestHub" }, "add_MyEvent", out actionInfo1, new IJsonValue[] { JTokenify("x") });
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(MyHubWithEvents), Name = "TestHub" }, "remove_MyEvent", out actionInfo2, new IJsonValue[] { JTokenify("x") });

            Assert.Null(actionInfo1);
            Assert.Null(actionInfo2);
        }

        [Fact]
        public void ResolveActionOnDerivedHubFindsMethodOnBasedType()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestDerivedHub), Name = "TestHub" }, "Foo", out actionInfo, new IJsonValue[] { });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(0, actionInfo.Parameters.Count);
        }

        [Fact]
        public void ResolveActionExcludesPropertiesOnDeclaredType()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "get_Value", out actionInfo, new IJsonValue[] { });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionExcludesPropetiesOnBaseTypes()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "get_Clients", out actionInfo, new IJsonValue[] { });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionLocatesPublicMethodsOnHub()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "Foo", out actionInfo, new IJsonValue[] { });

            Assert.NotNull(actionInfo);
            Assert.Equal("Foo", actionInfo.Name);
            Assert.Equal(0, actionInfo.Parameters.Count);
        }

        [Fact]
        public void ResolveActionReturnsNullIfMethodAmbiguous()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "Bar", out actionInfo, new[] { JTokenify(1) });

            Assert.Null(actionInfo);
        }

        [Fact]
        public void ResolveActionPicksMethodWithMatchingArguments()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "Foo", out actionInfo, new[] { JTokenify(1) });

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
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "MethodWithComplex", out actionInfo, new IJsonValue[] { new JTokenValue(arg) });

            Assert.NotNull(actionInfo);
            var complex = binder.ResolveMethodParameters(actionInfo, new[] { new JTokenValue(arg) })[0] as Complex;
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
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "MethodWithArray", out actionInfo, new IJsonValue[] { new JTokenValue(arg) });

            Assert.NotNull(actionInfo);
            var args = binder.ResolveMethodParameters(actionInfo, new[] { new JTokenValue(arg) })[0] as int[];
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
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "MethodWithArrayOfComplete", out actionInfo, new IJsonValue[] { new JTokenValue(new JArray(new object[] { arg })) });

            Assert.NotNull(actionInfo);
            var complexArray = binder.ResolveMethodParameters(actionInfo, new[] { new JTokenValue(new JArray(new object[] { arg })) })[0] as Complex[];
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
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "MethodWithGuid", out actionInfo, new IJsonValue[] { arg });

            Assert.NotNull(actionInfo);
            var arg0 = (Guid)binder.ResolveMethodParameters(actionInfo, new[] { arg })[0];
            Assert.Equal(new Guid("1d6a1d30-599f-4495-ace7-303fd87204bb"), arg0);
        }

        [Fact]
        public void ResolveActionBindsByteArray()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = JTokenify(Encoding.UTF8.GetBytes("Hello World!"));

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "MethodWithByteArray", out actionInfo, new IJsonValue[] { arg });

            Assert.NotNull(actionInfo);
            var arg0 = (byte[])binder.ResolveMethodParameters(actionInfo, new[] { arg })[0];
            Assert.Equal("Hello World!", Encoding.UTF8.GetString(arg0));
        }

        [Fact]
        public void ResolveActionBindsArrayOfByteArray()
        {
            var resolver = new ReflectedMethodDescriptorProvider();
            var binder = new DefaultParameterResolver();

            var arg = JTokenify(new[] { Encoding.UTF8.GetBytes("Hello World!") });

            MethodDescriptor actionInfo;
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "MethodListOfByteArray", out actionInfo, new IJsonValue[] { arg });

            Assert.NotNull(actionInfo);
            var arg0 = (List<byte[]>)binder.ResolveMethodParameters(actionInfo, new[] { arg })[0];
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
            resolver.TryGetMethod(new HubDescriptor { HubType = typeof(TestHub), Name = "TestHub" }, "MethodWithNullables", out actionInfo, new IJsonValue[] { arg1, arg2 });

            Assert.NotNull(actionInfo);
            var args = binder.ResolveMethodParameters(actionInfo, new[] { arg1, arg2 });
            Assert.Null(args[0]);
            Assert.Null(args[1]);
        }

        private IJsonValue JTokenify(object value)
        {
            return new JTokenValue(JToken.Parse(JsonConvert.SerializeObject(value)));
        }

        private class HubWithOverrides2 : IHub
        {

            public HubCallerContext Context
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public IHubCallerConnectionContext<dynamic> Clients
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public IGroupManager Groups
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public System.Threading.Tasks.Task OnConnected()
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task OnReconnected()
            {
                throw new NotImplementedException();
            }

            public System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        private class HubWithOverrides : Hub
        {
            public override System.Threading.Tasks.Task OnConnected()
            {
                return base.OnConnected();
            }

            public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
            {
                return base.OnDisconnected(stopCalled);
            }

            public override System.Threading.Tasks.Task OnReconnected()
            {
                return base.OnReconnected();
            }
        }

        private class TestDerivedHub : TestHub
        {
            public void FooDerived()
            {
                Foo();
            }
        }

        private class MyHubWithEvents : Hub
        {
            public event EventHandler MyEvent
            {
                add
                {
                }
                remove
                {
                }
            }
        }

        private class MyHubWithObjectMethods : Hub
        {
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }

            public override string ToString()
            {
                return base.ToString();
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
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

        private class JTokenValue : IJsonValue
        {
            private readonly JToken _value;

            public JTokenValue(JToken value)
            {
                _value = value;
            }

            public object ConvertTo(Type type)
            {
                // A non generic implementation of ToObject<T> on JToken
                using (var jsonReader = new JTokenReader(_value))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize(jsonReader, type);
                }
            }

            public bool CanConvertTo(Type type)
            {
                // TODO: Implement when we implement better method overload resolution
                return true;
            }
        }
    }
}
