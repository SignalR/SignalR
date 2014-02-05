// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class NullMethodDescriptor : MethodDescriptor
    {
        private static readonly IEnumerable<Attribute> _attributes = new List<Attribute>();
        private static readonly IList<ParameterDescriptor> _parameters = new List<ParameterDescriptor>();

        private readonly string _methodName;
        private readonly IEnumerable<MethodDescriptor> _availableMethods;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public NullMethodDescriptor(HubDescriptor descriptor, string methodName, IEnumerable<MethodDescriptor> availableMethods)
        {
            _methodName = methodName;
            _availableMethods = availableMethods;
            Hub = descriptor;
        }

        public override Func<IHub, object[], object> Invoker
        {
            get
            {
                return (emptyHub, emptyParameters) =>
                {
                    IEnumerable<string> availableMethodSignatures = GetAvailableMethodSignatures().ToArray();
                    var message = availableMethodSignatures.Any() ? 
                        String.Format(CultureInfo.CurrentCulture, Resources.Error_MethodCouldNotBeResolvedCandidates, _methodName, "\n" + String.Join("\n", availableMethodSignatures)) :
                        String.Format(CultureInfo.CurrentCulture, Resources.Error_MethodCouldNotBeResolved, _methodName);
                        
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, message));
                };
            }
        }

        private IEnumerable<string> GetAvailableMethodSignatures()
        {
            return _availableMethods.Select(m => m.Name + "(" + String.Join(", ", m.Parameters.Select(p => p.Name + ":" + p.ParameterType.Name)) + "):" + m.ReturnType.Name);
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
