using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.AspNet.SignalR.Client.Silverlight.Sample.ViewModels
{
    public class MainViewModel
    {
        public MainViewModel()
        {
            this.Items = new ObservableCollection<string>();
        }

        /// <summary>
        /// A collection for messages and notifications
        /// </summary>
        public ObservableCollection<string> Items { get; private set; }
    }
}