using Microsoft.ServiceBus.Notifications;
using SharedClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Serialization;

namespace ChatAppServer.Controllers
{
    public class ChatController : ApiController
    {
        // Create a  Notification Hub Client instance
        NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://sabbour.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=/kw+qf3qi3t4Qu/gtYMXyWA+iWErhCPUgd00kd6jB44=", "whatsupp");

        // POST api/chat/register
        [HttpPost]
        [ActionName("register")]
        public async Task<HttpResponseMessage> Register(Registration registration)
        {   
            // You'd also want to verify that the user in the registration is allowed to register for that tag

            // Get notification hubs registrations for that "username" tag.
            // Usually, you should get only 1, if we get more than one (for example, during development you make mistakes), delete all others
            // The "100" parameter just represents how many registrations to return
            var registrationsForUser = await hub.GetRegistrationsByTagAsync(registration.Username, 100);
            bool firstRegistration = true;
            bool existingRegistration = false;
            foreach (var reg in registrationsForUser)
            {
                if (firstRegistration)
                {
                    // Update the registration with the incoming channel
                    var winReg = reg as WindowsRegistrationDescription;
                    winReg.ChannelUri = new Uri(registration.Channel);
                    winReg.Tags = new HashSet<string> { registration.Username };
                    await hub.UpdateRegistrationAsync(winReg);

                    firstRegistration = false;
                    existingRegistration = true;
                }
                else
                {
                    // Delete other registrations for that user
                    await hub.DeleteRegistrationAsync(reg);
                }
            }

            // Register with the notification hub and listen to the "username" tag, if this is a new registration
            if (!existingRegistration)
            {
                switch (registration.Platform)
                {
                    case "WindowsStore":
                        await hub.CreateWindowsNativeRegistrationAsync(registration.Channel, new List<string> { registration.Username });
                        break;
                    default:
                        // If the platform isn't supported, return Bad Request
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, registration.Platform + " platform is not supported.");
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // POST api/chat/send
        [HttpPost]
        [ActionName("send")]
        public async Task<HttpResponseMessage> Send(ChatMessage message)
        {
            // Do verification that both users can send messages to each other
            // For example, they are friends

            // Then prepare the toast notification payload
            string toastPayload = "<toast> "
                          + "  <visual>"
                          + "      <binding template=\"ToastText02\">"
                          + "          <text id=\"1\">{0}</text>"
                          + "          <text id=\"2\">{1}</text>"
                          + "      </binding>"
                          + "  </visual>"
                          + "</toast>";

            toastPayload = string.Format(toastPayload, message.FromUsername, message.MessageText);

            // Send a Windows 8 toast notification to the "tag"
            await hub.SendWindowsNativeNotificationAsync(toastPayload,message.ToUsername);

            // Send a raw XML notification to update the UI as well in realtime, if the app is opened
            // Serialize the chat message into XML
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ChatMessage));
            StringWriter serializedStringWriter = new StringWriter();
            xmlSerializer.Serialize(serializedStringWriter, message);

            // Send the notification
            var rawChatNotification = new WindowsNotification(serializedStringWriter.ToString(), message.ToUsername);
            rawChatNotification.Headers.Add("X-WNS-Type", "wns/raw");
            rawChatNotification.ContentType = "application/octet-stream";
            await hub.SendNotificationAsync(rawChatNotification);

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
