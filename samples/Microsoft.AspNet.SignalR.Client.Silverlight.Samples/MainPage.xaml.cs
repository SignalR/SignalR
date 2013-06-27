using System.Net;
using System.Net.Browser;
using System.Threading;
using System.Windows.Controls;
using Microsoft.AspNet.SignalR.Client.Samples;

namespace Microsoft.AspNet.SignalR.Client.Silverlight.Samples
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();

            // http://msdn.microsoft.com/en-us/library/dd920295(v=vs.95).aspx
            // With Silverlight, you can specify whether the browser or the client provides HTTP handling for your Silverlight-based applications. 
            // By default, HTTP handling is performed by the browser and you must opt-in to client HTTP handling. 
            WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
            WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);

            var writer = new TextBoxWriter(SynchronizationContext.Current, this.Messages);
            var client = new CommonClient(writer);
            var task = client.RunAsync("http://localhost:40476/");
        }
    }
}
