// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "MVC and WebAPI don't seal their AuthorizeAttributes")]
    public class AuthorizeAttribute : Attribute, IAuthorizeHubConnection, IAuthorizeHubMethodInvocation
    {
        private string _roles;
        private string[] _rolesSplit = new string[0];
        private string _users;
        private string[] _usersSplit = new string[0];

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Already somewhat represented by set-only RequiredOutgoing property.")]
        protected bool? _requireOutgoing;

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Must be property because this is an attribute parameter.")]
        public bool RequireOutgoing
        {
            // It is impossible to tell here whether the attribute is being applied to a method or class. This makes
            // it impossible to determine whether the value should be true or false when _requireOutgoing is null.
            // It is also impossible to have a Nullable attribute parameter type.
            get { throw new NotImplementedException(Resources.Error_DoNotReadRequireOutgoing); }
            set { _requireOutgoing = value; }
        }

        public string Roles
        {
            get { return _roles ?? String.Empty; }
            set
            {
                _roles = value;
                _rolesSplit = SplitString(value);
            }
        }

        public string Users
        {
            get { return _users ?? String.Empty; }
            set
            {
                _users = value;
                _usersSplit = SplitString(value);
            }
        }

        public virtual bool AuthorizeHubConnection(HubDescriptor hubDescriptor, IRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            // If RequireOutgoing is explicitly set to false, authorize all connections.
            if (_requireOutgoing.HasValue && !_requireOutgoing.Value)
            {
                return true;
            }

            return UserAuthorized(request.User);
        }

        public virtual bool AuthorizeHubMethodInvocation(IHubIncomingInvokerContext hubIncomingInvokerContext, bool appliesToMethod)
        {
            if (hubIncomingInvokerContext == null)
            {
                throw new ArgumentNullException("hubIncomingInvokerContext");
            }

            // It is impossible to require outgoing auth at the method level with SignalR's current design.
            // Even though this isn't the stage at which outgoing auth would be applied, we want to throw a runtime error
            // to indicate when the attribute is being used with obviously incorrect expectations.

            // We must explicitly check if _requireOutgoing is true since it is a Nullable type.
            if (appliesToMethod && (_requireOutgoing == true))
            {
                throw new ArgumentException(Resources.Error_MethodLevelOutgoingAuthorization);
            }

            return UserAuthorized(hubIncomingInvokerContext.Hub.Context.User);
        }

        protected virtual bool UserAuthorized(IPrincipal user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (!user.Identity.IsAuthenticated)
            {
                return false;
            }

            if (_usersSplit.Length > 0 && !_usersSplit.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (_rolesSplit.Length > 0 && !_rolesSplit.Any(user.IsInRole))
            {
                return false;
            }

            return true;
        }

        private static string[] SplitString(string original)
        {
            if (String.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = from piece in original.Split(',')
                        let trimmed = piece.Trim()
                        where !String.IsNullOrEmpty(trimmed)
                        select trimmed;
            return split.ToArray();
        }
    }
}
