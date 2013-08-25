using Newtonsoft.Json;
using SharedClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.PushNotifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace ChatAppW8
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : ChatAppW8.Common.LayoutAwarePage
    {
        // Collection to hold the messages
        ObservableCollection<ChatMessage> ChatMessages { get; set; }

        public static string baseUrl = "http://localhost:20250";

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (pageState == null)
            {
                ChatMessages = new ObservableCollection<ChatMessage>();
                // Initialize the message list to an empty list initially
                messagesListView.ItemsSource = ChatMessages;
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {

        }

        private async void registerButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable button
            registerButton.IsEnabled = false;
            yourUsernameTextBox.IsEnabled = false;

            // Obtain a channel
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            channel.PushNotificationReceived += channel_PushNotificationReceived;


            var registration = new Registration
            {
                Username = yourUsernameTextBox.Text,
                Channel = channel.Uri,
                Platform = "WindowsStore"
            };

            // Call our server to register this channel
            using (var httpClient = new HttpClient())
            {
                var serializedBody = JsonConvert.SerializeObject(registration);
                await httpClient.PostAsync(baseUrl + "/api/chat/register", new StringContent(serializedBody, Encoding.UTF8, "application/json"));
            }

            // Show the messages panel
            registerButton.Content = "Ok!";
            messagesPanel.Visibility = Visibility.Visible;
        }

        private async void sendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            sendMessageButton.IsEnabled = false;
            var message = new ChatMessage
            {
                FromUsername = yourUsernameTextBox.Text,
                ToUsername = toUsernameTextBox.Text,
                MessageText = messageTextBox.Text
            };

            // Add the message to our UI before sending it out
            ChatMessages.Add(message);

            // Call our server send a message
            using (var httpClient = new HttpClient())
            {
                var serializedBody = JsonConvert.SerializeObject(message);
                await httpClient.PostAsync(baseUrl + "/api/chat/send", new StringContent(serializedBody, Encoding.UTF8, "application/json"));
            }

            // Clean up UI
            sendMessageButton.IsEnabled = true;
            messageTextBox.Text = string.Empty;
        }

        // Handles the received push notifications, and updates the UI in case it was a Raw notification
        async void channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            if (args.NotificationType == PushNotificationType.Raw)
            {
                // Deserialize into a ChatMessage object
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ChatMessage));
                StringReader stringReader = new StringReader(args.RawNotification.Content);
                var chatMessage = xmlSerializer.Deserialize(stringReader) as ChatMessage;

                // Add to the chat messages, has to run on the UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    ()=>ChatMessages.Add(chatMessage)
               );
            }
        }
    }
}
