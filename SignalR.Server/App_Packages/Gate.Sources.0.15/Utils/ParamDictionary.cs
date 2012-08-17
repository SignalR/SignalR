using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gate.Utils
{
    internal class ParamDictionary : IDictionary<string, string>
    {
        static readonly char[] DefaultParamSeparators = new[] { '&', ';' };
        static readonly char[] ParamKeyValueSeparator = new[] { '=' };
        static readonly char[] LeadingWhitespaceChars = new[] { ' ' };

        public static IEnumerable<KeyValuePair<string, string>> ParseToEnumerable(string queryString, char[] delimiters)
        {
            var items = (queryString ?? "").Split(delimiters ?? DefaultParamSeparators, StringSplitOptions.RemoveEmptyEntries);
            var rawPairs = items.Select(item => item.Split(ParamKeyValueSeparator, 2, StringSplitOptions.None));
            var pairs = rawPairs.Select(pair => new KeyValuePair<string, string>(
                Uri.UnescapeDataString(pair[0]).TrimStart(LeadingWhitespaceChars),
                pair.Length < 2 ? "" : Uri.UnescapeDataString(pair[1])));
            return pairs;
        }

        public static IDictionary<string, string> Parse(string queryString, char[] delimiters = null)
        {
            var d = ParseToEnumerable(queryString, delimiters)
                .GroupBy(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => string.Join(",", g.ToArray()), StringComparer.OrdinalIgnoreCase);

            return new ParamDictionary(d);
        }

        readonly IDictionary<string, string> _impl;



        ParamDictionary(IDictionary<string, string> impl)
        {
            _impl = impl;
        }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            return _impl.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _impl.GetEnumerator();
        }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            _impl.Add(item);
        }

        void ICollection<KeyValuePair<string, string>>.Clear()
        {
            _impl.Clear();
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            return _impl.Contains(item);
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _impl.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            return _impl.Remove(item);
        }

        int ICollection<KeyValuePair<string, string>>.Count
        {
            get { return _impl.Count; }
        }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly
        {
            get { return _impl.IsReadOnly; }
        }

        bool IDictionary<string, string>.ContainsKey(string key)
        {
            return _impl.ContainsKey(key);
        }

        void IDictionary<string, string>.Add(string key, string value)
        {
            _impl.Add(key, value);
        }

        bool IDictionary<string, string>.Remove(string key)
        {
            return _impl.Remove(key);
        }

        bool IDictionary<string, string>.TryGetValue(string key, out string value)
        {
            return _impl.TryGetValue(key, out value);
        }

        string IDictionary<string, string>.this[string key]
        {
            get
            {
                string value;
                return _impl.TryGetValue(key, out value) ? value : default(string);
            }
            set { _impl[key] = value; }
        }

        ICollection<string> IDictionary<string, string>.Keys
        {
            get { return _impl.Keys; }
        }

        ICollection<string> IDictionary<string, string>.Values
        {
            get { return _impl.Values; }
        }
    }
}