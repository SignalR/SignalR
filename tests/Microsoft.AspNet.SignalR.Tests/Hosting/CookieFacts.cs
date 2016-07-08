// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Hosting
{
    public class CookieFacts
    {
        [Fact]
        public void CookiePropertiesAreSetWhenInitializedThroughCtor()
        {
            // Arrange
            var cookie = new Cookie("name", "value", "www.foo.com", "/");

            // Assert
            Assert.Equal("name", cookie.Name);
            Assert.Equal("value", cookie.Value);
            Assert.Equal("www.foo.com", cookie.Domain);
            Assert.Equal("/", cookie.Path);
        }
    }
}
