﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// The response returned from an incoming hub request.
    /// </summary>
    public class HubResponse
    {
        /// <summary>
        /// The changes made the the round tripped state.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Type is used for serialization")]
        [JsonProperty("S", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> State { get; set; }

        /// <summary>
        /// The result of the invocation.
        /// </summary>
        [JsonProperty("R", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        /// <summary>
        /// The id of the operation.
        /// </summary>
        [JsonProperty("I")]
        public string Id { get; set; }

        /// <summary>
        /// The progress update of the invocation.
        /// </summary>
        [JsonProperty("P", NullValueHandling = NullValueHandling.Ignore)]
        public object Progress { get; set; }

        /// <summary>
        /// Indicates whether the Error is a see <see cref="HubException"/>.
        /// </summary>
        [JsonProperty("H", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsHubException { get; set; }

        /// <summary>
        /// The exception that occurs as a result of invoking the hub method.
        /// </summary>
        [JsonProperty("E", NullValueHandling = NullValueHandling.Ignore)]
        public string Error { get; set; }

        /// <summary>
        /// The stack trace of the exception that occurs as a result of invoking the hub method.
        /// </summary>
        [JsonProperty("T", NullValueHandling = NullValueHandling.Ignore)]
        public string StackTrace { get; set; }

        /// <summary>
        /// Extra error data contained in the <see cref="HubException"/>
        /// </summary>
        [JsonProperty("D", NullValueHandling = NullValueHandling.Ignore)]
        public object ErrorData { get; set; }
    }
}
