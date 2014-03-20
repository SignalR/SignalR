using Microsoft.AspNet.SignalR.Client.WP8.Samples.Resources;

namespace Microsoft.AspNet.SignalR.Client.WP8.Samples
{
    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources { get { return _localizedResources; } }
    }
}