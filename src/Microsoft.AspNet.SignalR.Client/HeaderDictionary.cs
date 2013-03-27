using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.SignalR.Client
{
    public class HeaderDictionary : IDictionary<string, string>
    {
        private IDictionary<string, string> _dictionary = new Dictionary<string, string>();
        private readonly IConnection _connection;

        public HeaderDictionary(IConnection connection)
        {
            _connection = connection;
        }

        public void Add(string key, string value)
        {
            EnsureConnnectionDisconnected();
            _dictionary.Add(key, value);
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
            EnsureConnnectionDisconnected();
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
                EnsureConnnectionDisconnected();
                _dictionary[key] = value;
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            EnsureConnnectionDisconnected();
            _dictionary.Add(item);
        }

        public void Clear()
        {
            EnsureConnnectionDisconnected();
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
            get { return _connection.State != ConnectionState.Disconnected || _dictionary.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            EnsureConnnectionDisconnected();
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

        private void EnsureConnnectionDisconnected()
        {
            if (_connection.State != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException(Resources.Error_HeadersCanOnlyBeSetWhenDisconnected);
            }
        }
    }
}
