// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNet.SignalR.Owin.Infrastructure
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "It is instantiated in the static Parse method")]
    internal sealed class ParamDictionary : IDictionary<string, string>
    {
        private static readonly char[] DefaultParamSeparators = new[] { '&', ';' };
        private static readonly char[] ParamKeyValueSeparator = new[] { '=' };
        private static readonly char[] LeadingWhitespaceChars = new[] { ' ' };

        internal static IEnumerable<KeyValuePair<string, string>> ParseToEnumerable(string value, char[] delimiters = null)
        {
            value = value ?? String.Empty;
            delimiters = delimiters ?? DefaultParamSeparators;

            var items = value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in items)
            {
                string[] pair = item.Split(ParamKeyValueSeparator, 2, StringSplitOptions.None);

                string pairKey = Escape(pair[0]).TrimStart(LeadingWhitespaceChars);
                string pairValue = pair.Length < 2 ? String.Empty : Escape(pair[1]);

                yield return new KeyValuePair<string, string>(pairKey, pairValue);
            }
        }
        
        private readonly IDictionary<string, string> _values;

        internal ParamDictionary(IDictionary<string, string> values)
        {
            _values = values;
        }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            _values.Add(item);
        }

        void ICollection<KeyValuePair<string, string>>.Clear()
        {
            _values.Clear();
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            return _values.Contains(item);
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            return _values.Remove(item);
        }

        int ICollection<KeyValuePair<string, string>>.Count
        {
            get { return _values.Count; }
        }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly
        {
            get { return _values.IsReadOnly; }
        }

        bool IDictionary<string, string>.ContainsKey(string key)
        {
            return _values.ContainsKey(key);
        }

        void IDictionary<string, string>.Add(string key, string value)
        {
            _values.Add(key, value);
        }

        bool IDictionary<string, string>.Remove(string key)
        {
            return _values.Remove(key);
        }

        bool IDictionary<string, string>.TryGetValue(string key, out string value)
        {
            return _values.TryGetValue(key, out value);
        }

        string IDictionary<string, string>.this[string key]
        {
            get
            {
                string value;
                if (_values.TryGetValue(key, out value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                _values[key] = value;
            }
        }

        ICollection<string> IDictionary<string, string>.Keys
        {
            get { return _values.Keys; }
        }

        ICollection<string> IDictionary<string, string>.Values
        {
            get { return _values.Values; }
        }

        private static string Escape(string value)
        {
            return Uri.UnescapeDataString(value).Replace('+', ' ');
        }
    }
}
