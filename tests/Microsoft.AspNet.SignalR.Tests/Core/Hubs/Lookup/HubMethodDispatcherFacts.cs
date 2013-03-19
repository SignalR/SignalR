// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class HubMethodDispatcherFacts
    {
        [Fact]
        public void ExecuteWithNormalHubMethod()
        {
            // Arrange
            DispatcherHub hub = new DispatcherHub();
            object[] parameters = new object[] { 5, "some string", new DateTime(2001, 1, 1) };
            MethodInfo methodInfo = typeof(DispatcherHub).GetMethod("NormalHubMethod");
            HubMethodDispatcher dispatcher = new HubMethodDispatcher(methodInfo);

            // Act
            object returnValue = dispatcher.Execute(hub, parameters);

            // Assert
            var stringResult = Assert.IsType<string>(returnValue);
            Assert.Equal("Hello from NormalHubMethod!", stringResult);

            Assert.Equal(5, hub._i);
            Assert.Equal("some string", hub._s);
            Assert.Equal(new DateTime(2001, 1, 1), hub._dt);
        }

        [Fact]
        public void ExecuteWithParameterlessHubMethod()
        {
            // Arrange
            DispatcherHub hub = new DispatcherHub();
            object[] parameters = new object[0];
            MethodInfo methodInfo = typeof(DispatcherHub).GetMethod("ParameterlessHubMethod");
            HubMethodDispatcher dispatcher = new HubMethodDispatcher(methodInfo);

            // Act
            object returnValue = dispatcher.Execute(hub, parameters);

            // Assert
            var intResult = Assert.IsType<int>(returnValue);
            Assert.Equal(53, intResult);
        }

        [Fact]
        public void ExecuteWithStaticHubMethod()
        {
            // Arrange
            DispatcherHub hub = new DispatcherHub();
            object[] parameters = new object[0];
            MethodInfo methodInfo = typeof(DispatcherHub).GetMethod("StaticHubMethod");
            HubMethodDispatcher dispatcher = new HubMethodDispatcher(methodInfo);

            // Act
            object returnValue = dispatcher.Execute(hub, parameters);

            // Assert
            var intResult = Assert.IsType<int>(returnValue);
            Assert.Equal(89, intResult);
        }

        [Fact]
        public void ExecuteWithVoidHubMethod()
        {
            // Arrange
            DispatcherHub hub = new DispatcherHub();
            object[] parameters = new object[] { 5, "some string", new DateTime(2001, 1, 1) };
            MethodInfo methodInfo = typeof(DispatcherHub).GetMethod("VoidHubMethod");
            HubMethodDispatcher dispatcher = new HubMethodDispatcher(methodInfo);

            // Act
            object returnValue = dispatcher.Execute(hub, parameters);

            // Assert
            Assert.Null(returnValue);
            Assert.Equal(5, hub._i);
            Assert.Equal("some string", hub._s);
            Assert.Equal(new DateTime(2001, 1, 1), hub._dt);
        }

        [Fact]
        public void MethodInfoProperty()
        {
            // Arrange
            MethodInfo original = typeof(object).GetMethod("ToString");
            HubMethodDispatcher dispatcher = new HubMethodDispatcher(original);

            // Act
            MethodInfo returned = dispatcher.MethodInfo;

            // Assert
            Assert.Same(original, returned);
        }

        private class DispatcherHub : Hub
        {
            public int _i;
            public string _s;
            public DateTime _dt;

            public object NormalHubMethod(int i, string s, DateTime dt)
            {
                VoidHubMethod(i, s, dt);
                return "Hello from NormalHubMethod!";
            }

            public int ParameterlessHubMethod()
            {
                return 53;
            }

            public void VoidHubMethod(int i, string s, DateTime dt)
            {
                _i = i;
                _s = s;
                _dt = dt;
            }

            public static int StaticHubMethod()
            {
                return 89;
            }
        }
    }
}
