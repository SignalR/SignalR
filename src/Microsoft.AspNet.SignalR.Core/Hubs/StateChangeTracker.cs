// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// A change tracking dictionary.
    /// </summary>
    public class StateChangeTracker
    {
        private readonly JObject _values;
        // Keep track of everyting that changed since creation
        private readonly IDictionary<string, object> _oldValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public StateChangeTracker()
        {
            _values = new JObject();
        }

        public StateChangeTracker(JObject values)
        {
            _values = values;
        }

        public object this[string key]
        {
            get
            {
                JToken result;
                _values.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out result);
                return result == null ? null : result.Value<object>();
            }
            set
            {
                if (!_oldValues.ContainsKey(key))
                {
                    JToken oldValue;
                    _values.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out oldValue);
                    _oldValues[key] = oldValue == null ? null : oldValue.Value<object>();
                }

                _values[key] = JToken.FromObject(value);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        public IDictionary<string, object> GetChanges()
        {
            var changes = (from key in _oldValues.Keys
                           let oldValue = _oldValues[key]
                           let newValue = _values[key]
                           where !Object.Equals(oldValue, newValue)
                           select new
                           {
                               Key = key,
                               Value = newValue.Value<object>()
                           }).ToDictionary(p => p.Key, p => p.Value);

            return changes.Count > 0 ? changes : null;
        }
    }
}
