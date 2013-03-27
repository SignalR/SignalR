using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.SignalR.Client
{
    public class CustomHeaderDictionary : IDictionary<string, string>
    {
        private IDictionary<string, string> _dictionary = new Dictionary<string, string>();
        private IConnection _conn;

        public CustomHeaderDictionary(IConnection connection)
        {
            _conn = connection;
        }

        public void Add(string key, string value)
        {
            if (_conn.State == ConnectionState.Disconnected)
            {
                _dictionary[key] = value;
            }
            else
            {
                throw new InvalidOperationException(Resources.Error_HeadersCanOnlyBeSetWhenDisconnected);
            }
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return _dictionary.Keys; }
        }

        public bool Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public ICollection<string> Values
        {
            get { return _dictionary.Values; }
        }

        public string this[string key]
        {
            get
            {
                return _dictionary[key];
            }
            set
            {
                if (_conn.State == ConnectionState.Disconnected)
                {
                    _dictionary.Add(key, value);
                }
                else
                {
                    throw new InvalidOperationException(Resources.Error_HeadersCanOnlyBeSetWhenDisconnected);
                }
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _dictionary.Add(item);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return _dictionary.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _dictionary.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
    }
}
