using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalR.Hubs
{
    /// <summary>
    /// A change tracking dictionary.
    /// </summary>
    public class TrackingDictionary
    {
        private readonly IDictionary<string, object> _values;
        // Keep track of everyting that changed since creation
        private readonly IDictionary<string, object> _oldValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public TrackingDictionary()
        {
            _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public TrackingDictionary(IDictionary<string, object> values)
        {
            _values = values;
        }

        public object this[string key]
        {
            get
            {
                object result;
                _values.TryGetValue(key, out result);
                return result;
            }
            set
            {
                if (!_oldValues.ContainsKey(key))
                {
                    object oldValue;
                    _values.TryGetValue(key, out oldValue);
                    _oldValues[key] = oldValue;
                }

                _values[key] = value;
            }
        }

        public IDictionary<string, object> GetChanges()
        {
            var changes = (from key in _oldValues.Keys
                           let oldValue = _oldValues[key]
                           let newValue = _values[key]
                           where !Object.Equals(oldValue, newValue)
                           select new
                           {
                               Key = key,
                               Value = newValue
                           }).ToDictionary(p => p.Key, p => p.Value);

            return changes;
        }
    }
}
