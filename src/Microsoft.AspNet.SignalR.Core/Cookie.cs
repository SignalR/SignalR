// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR
{
    public class Cookie
    {
        public Cookie(string name, string value)
            : this(name, value, String.Empty, String.Empty)
        {

        }

        public Cookie(string name, string value, string domain, string path)
        {
            Name = name;
            Value = value;
            Domain = domain;
            Path = path;
        }

        public string Name { get; private set; }
        public string Domain { get; private set; }
        public string Path { get; private set; }
        public string Value { get; private set; }
    }
}
