namespace SignalR
{
    /// <summary>
    /// All values saved to the messages store are wrapped by this type.
    /// If a store needs to save values in a serializable way then it just needs to call
    /// ToString() and we'll unwrap it when it comes back (if needed).
    /// </summary>
    internal struct WrappedValue
    {
        private readonly IJsonSerializer _serializer;
        private readonly object _value;

        public WrappedValue(object value, IJsonSerializer serializer)
        {
            _value = value;
            _serializer = serializer;
        }

        public object Value
        {
            get
            {
                return _value;
            }
        }

        public static T Unwrap<T>(object value, IJsonSerializer serializer)
        {
            if (value is WrappedValue)
            {
                return (T)((WrappedValue)value).Value;
            }
            
            return serializer.Parse<T>((string)value);
        }

        public static object Unwrap(object value, IJsonSerializer serializer)
        {
            if (value is WrappedValue)
            {
                return ((WrappedValue)value).Value;
            }
            
            return serializer.Parse((string)value);
        }

        public override string ToString()
        {
            return _serializer.Stringify(_value);
        }
    }
}
