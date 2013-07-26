using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    internal class OwinEnvironment : IDictionary<string, object>
    {
        private IDictionary<string, object> _extraProperties;
        private IDictionary<string, object> _hostProperties;

        private string _requestProtocol;
        private CancellationToken _callCancelled;
        private string _requestMethod;
        private string _requestPathBase;
        private string _requestPath;
        private string _requestQueryString;
        private string _requestScheme;
        private Stream _requestBody;
        private IDictionary<string, string[]> _requestHeaders;

        private IDictionary<string, string[]> _responseHeaders;
        private int _responseStatusCode;
        private Stream _responseBody;

        private readonly string[] _defaultKeys = new[]
        {
            OwinConstants.Version,
            OwinConstants.RequestProtocol,
            OwinConstants.CallCancelled,
            OwinConstants.RequestMethod,
            OwinConstants.RequestPathBase,
            OwinConstants.RequestPath,
            OwinConstants.RequestQueryString,
            OwinConstants.RequestScheme,
            OwinConstants.RequestHeaders,
            OwinConstants.RequestBody,
            OwinConstants.ResponseHeaders,
            OwinConstants.ResponseStatusCode,
            OwinConstants.ResponseBody,
        };

        public OwinEnvironment(IDictionary<string, object> hostProperties)
        {
            _hostProperties = hostProperties;
        }

        private IDictionary<string, object> ExtraProperties
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _extraProperties, () => new Dictionary<string, object>());
            }
        }

        public void Add(string key, object value)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            object value;
            return TryGetValue(key, out value);
        }

        public ICollection<string> Keys
        {
            get
            {
                if (_extraProperties == null)
                {
                    return _defaultKeys;
                }
                return _defaultKeys.Concat(_extraProperties.Keys).Concat(_hostProperties.Keys).ToArray();
            }
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out object value)
        {
            if (key == OwinConstants.Version)
            {
                value = "1.0";
                return true;
            }

            switch (key)
            {
                case OwinConstants.RequestProtocol:
                    value = _requestProtocol;
                    return true;
                case OwinConstants.CallCancelled:
                    value = _callCancelled;
                    return true;
                case OwinConstants.RequestMethod:
                    value = _requestMethod;
                    return true;
                case OwinConstants.RequestPathBase:
                    value = _requestPathBase;
                    return true;
                case OwinConstants.RequestPath:
                    value = _requestPath;
                    return true;
                case OwinConstants.RequestQueryString:
                    value = _requestQueryString;
                    return true;
                case OwinConstants.RequestScheme:
                    value = _requestScheme;
                    return true;
                case OwinConstants.RequestHeaders:
                    value = _requestHeaders;
                    return true;
                case OwinConstants.RequestBody:
                    value = _requestBody;
                    return true;
                case OwinConstants.ResponseHeaders:
                    value = _responseHeaders;
                    return true;
                case OwinConstants.ResponseStatusCode:
                    value = _responseStatusCode;
                    return true;
                case OwinConstants.ResponseBody:
                    value = _responseBody;
                    return true;
                default:
                    if (ExtraProperties.TryGetValue(key, out value))
                    {
                        return true;
                    }
                    else
                    {
                        return _hostProperties.TryGetValue(key, out value);
                    }
            }
        }

        public ICollection<object> Values
        {
            get { throw new NotImplementedException(); }
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "The case is required")]
        public object this[string key]
        {
            get
            {
                object value;
                TryGetValue(key, out value);
                return value;
            }
            set
            {
                switch (key)
                {
                    case OwinConstants.RequestProtocol:
                        _requestProtocol = (string)value;
                        break;
                    case OwinConstants.CallCancelled:
                        _callCancelled = (CancellationToken)value;
                        break;
                    case OwinConstants.RequestMethod:
                        _requestMethod = (string)value;
                        break;
                    case OwinConstants.RequestPathBase:
                        _requestPathBase = (string)value;
                        break;
                    case OwinConstants.RequestPath:
                        _requestPath = (string)value;
                        break;
                    case OwinConstants.RequestQueryString:
                        _requestQueryString = (string)value;
                        break;
                    case OwinConstants.RequestScheme:
                        _requestScheme = (string)value;
                        break;
                    case OwinConstants.RequestHeaders:
                        _requestHeaders = (IDictionary<string, string[]>)value;
                        break;
                    case OwinConstants.RequestBody:
                        _requestBody = (Stream)value;
                        break;
                    case OwinConstants.ResponseHeaders:
                        _responseHeaders = (IDictionary<string, string[]>)value;
                        break;
                    case OwinConstants.ResponseStatusCode:
                        _responseStatusCode = (int)value;
                        break;
                    case OwinConstants.ResponseBody:
                        _responseBody = (Stream)value;
                        break;
                    default:
                        ExtraProperties[key] = value;
                        break;
                }
            }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                if (_extraProperties == null)
                {
                    return _defaultKeys.Length;
                }
                return _defaultKeys.Length + ExtraProperties.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
