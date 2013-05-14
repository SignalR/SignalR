using System.Threading;
using System.Windows.Controls;
using Microsoft.AspNet.SignalR.Client.Samples;

namespace Microsoft.AspNet.SignalR.Client.Silverlight5.Samples
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();

            var writer = new TextBoxWriter(SynchronizationContext.Current, this.Messages);
            var client = new CommonClient(writer);
            client.RunAsync();
        }
    }
}
