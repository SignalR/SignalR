using System.Collections.Generic;

namespace System.Collections.Generic
{
    static class DictionaryExtensions
    {
        // Retrieves the value if present, or returns the default (null) otherwise.
        public static T Get<T>(this IDictionary<string, object> dictionary, string key)
        {
            object value;
            return dictionary.TryGetValue(key, out value) ? (T)value : default(T);
        }

        // Sets the value if non-null, or removes it otherwise
        public static void Set<T>(this IDictionary<string, object> dictionary, string key, T value)
        {
            if (object.Equals(value, default(T)))
            {
                dictionary.Remove(key);
            }
            else
            {
                dictionary[key] = value;
            }
        }
    }
}