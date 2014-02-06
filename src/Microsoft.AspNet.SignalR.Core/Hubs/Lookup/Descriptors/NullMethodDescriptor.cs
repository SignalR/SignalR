﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class NullMethodDescriptor : MethodDescriptor
    {
        private static readonly IEnumerable<Attribute> _attributes = new List<Attribute>();
        private static readonly IList<ParameterDescriptor> _parameters = new List<ParameterDescriptor>();

        private string _methodName;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public NullMethodDescriptor(HubDescriptor descriptor, string methodName)
        {
            _methodName = methodName;
            Hub = descriptor;
        }

        public override Func<IHub, object[], object> Invoker
        {
            get
            {
                return (emptyHub, emptyParameters) =>
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_MethodCouldNotBeResolved, _methodName));
                };
            }
        }

        public override IList<ParameterDescriptor> Parameters
        {
            get { return _parameters; }
        }

        public override IEnumerable<Attribute> Attributes 
        {
            get { return _attributes; }
        }
    }
}
