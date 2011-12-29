namespace SignalR.Abstractions
{
    public static class HostContextExtensions
    {
        public static T GetValue<T>(this HostContext context, string key)
        {
            object value;
            if (context.Items.TryGetValue(key, out value))
            {
                return (T)value;
            }
            return default(T);
        }

        public static bool IsDebuggingEnabled(this HostContext context)
        {
            return context.GetValue<bool>("debugMode");
        }
    }
}
