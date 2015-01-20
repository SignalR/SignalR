namespace Microsoft.AspNet.SignalR.Client.LibExtensions
{
    static class StringHelper
    {
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;
            return string.IsNullOrEmpty(value.Trim());
        }
    }
}