using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNet.SignalR.Client.Silverlight.Sample.ViewModels;

namespace Microsoft.AspNet.SignalR.Client.Silverlight.Sample
{
    public partial class MainPage : UserControl
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;

            var connection = new Connection("http://localhost:40476/raw-connection");

            connection.Received += data =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.Items.Add(data);
                });
            };

            connection.Error += ex =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    var aggEx = (AggregateException)ex;
                    App.ViewModel.Items.Add(aggEx.InnerExceptions[0].Message);
                });
            };

            connection.Reconnected += () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.Items.Add("Connection restored");
                });
            };

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            connection.TraceWriter = new Writer(val =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.Items.Add(val);
                });
            });

            connection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    var ex = task.Exception.InnerExceptions[0];
                    App.ViewModel.Items.Add(ex.Message);
                }
                else
                {
                    App.ViewModel.Items.Add("Connected with transport " + connection.Transport.Name);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            scheduler);
        }

        private class Writer : TextWriter
        {
            private readonly Action<string> _log;
            public Writer(Action<string> log)
            {
                _log = log;
            }

            public override void WriteLine(string value)
            {
                _log(value);
            }

            public override System.Text.Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }
        }
    }
}
