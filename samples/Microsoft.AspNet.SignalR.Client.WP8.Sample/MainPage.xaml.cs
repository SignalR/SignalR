using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.WP8.Sample.ViewModels;
using Microsoft.Phone.Controls;

namespace Microsoft.AspNet.SignalR.Client.WP8.Sample
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            var connection = new HubConnection("http://signalr-sample.azurewebsites.net");

            var hub = connection.CreateHubProxy("statushub");

            hub.On<string>("joined", data =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.Items.Add(new ItemViewModel { LineOne = data });
                });
            });

            connection.Error += ex =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    var aggEx = (AggregateException)ex;
                    App.ViewModel.Items.Add(new ItemViewModel { LineOne = aggEx.InnerExceptions[0].Message });
                });
            };

            connection.Reconnected += () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.Items.Add(new ItemViewModel { LineOne = "Connection restored" });
                });
            };

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            connection.Start().ContinueWith(task =>
            {
                var ex = task.Exception.InnerExceptions[0];
                App.ViewModel.Items.Add(new ItemViewModel { LineOne = ex.Message });
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            scheduler);
        }

        // Handle selection changed on ListBox
        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (MainListBox.SelectedIndex == -1)
                return;

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?selectedItem=" + MainListBox.SelectedIndex, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            MainListBox.SelectedIndex = -1;
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}