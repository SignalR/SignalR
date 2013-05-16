using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.AspNet.SignalR.Client.Samples;

namespace Microsoft.AspNet.SignalR.Client.Silverlight.Samples
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();

            var writer = new TextBlockWriter(SynchronizationContext.Current, this.Messages);
            var client = new CommonClient(writer);
            client.RunAsync();
        }
    }
}
