using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Notifications;
using Microsoft.WindowsAzure.Mobile.Service;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace CrmBandNotificationsService
{
    public class ProcessCrmQueueJob : ScheduledJob
    {
        //TODO: Populate based on your Azure instance
        private const string IssuerName = "Azure Service Bus - CRM Namespace - Default Issuer";
        private const string IssuerSecret = "Azure Service Bus - CRM Namespace - Default Key";
        private const string ServiceNamespace = "Azure Service Bus - CRM Namespace Name";
        private const string QueueName = "Azure Service Bus - CRM Namespace - Queue Name";
        private const string NotificationHubConnectionString = "Azure Service Bus - Notification Hub - DefaultFullSharedAccessSignature";
        private const string NotificationHubName = "Azure Service Bus - Notification Hub - Name";
        public override Task ExecuteAsync()
        {
            TokenProvider credentials = TokenProvider.CreateSharedSecretTokenProvider(IssuerName, IssuerSecret);

            MessagingFactory factory = MessagingFactory.Create(ServiceBusEnvironment.CreateServiceUri("sb", ServiceNamespace, string.Empty), credentials);
            QueueClient myQueueClient = factory.CreateQueueClient(QueueName);

            //Get the message from the queue
            BrokeredMessage message;
            while ((message = myQueueClient.Receive(new TimeSpan(0, 0, 5))) != null)
            {
                Stream stream = message.GetBody<Stream>();
                StreamReader reader = new StreamReader(stream);
                string s = reader.ReadToEnd();

                XmlDocument document = new XmlDocument();
                document.LoadXml(s); 
                XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
                manager.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
                manager.AddNamespace("a", "http://www.w3.org/2005/08/addressing");
                manager.AddNamespace("i", "http://www.w3.org/2001/XMLSchema-instance");
                manager.AddNamespace("b", "http://schemas.datacontract.org/2004/07/System.Collections.Generic");
                manager.AddNamespace("c", "http://www.w3.org/2001/XMLSchema");
                manager.AddNamespace("r", "http://schemas.microsoft.com/xrm/2011/Contracts");

                XmlNode envelope = document.SelectSingleNode("s:Envelope", manager);
                XmlNode body = envelope.SelectSingleNode("s:Body", manager);
                XmlNode rec = body.SelectSingleNode("r:RemoteExecutionContext", manager);
                XmlNode ip = rec.SelectSingleNode("r:InputParameters", manager);
                XmlNode kvp = ip.SelectSingleNode("r:KeyValuePairOfstringanyType", manager);
                XmlNode val = kvp.SelectSingleNode("b:value", manager);
                XmlNode attr = val.FirstChild;
                XmlNodeList kvps = attr.ChildNodes;

                string ownerId = string.Empty;
                string subject = string.Empty;
                foreach (XmlNode node in kvps)
                {
                    XmlNode k = node.SelectSingleNode("b:key", manager);
                    if (k.InnerText == "lat_recipient")
                    {
                        XmlNode value = node.SelectSingleNode("b:value", manager);
                        ownerId = value.FirstChild.InnerText;
                    }
                    if (k.InnerText == "lat_message")
                    {
                        XmlNode value = node.SelectSingleNode("b:value", manager);
                        subject = value.LastChild.InnerText;
                    }
                }

                SendNotificationAsync(subject, ownerId);
                message.Complete();
            }

            return Task.FromResult(true);
        }

        private static async void SendNotificationAsync(string message, string userid)
        {
            NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString(
                NotificationHubConnectionString,
                NotificationHubName);

            //content-available = 1 makes sure ReceivedRemoteNotification in 
            //AppDelegate.cs get executed when the app is closed
            var alert = "{\"aps\":{\"alert\":\"" + message + "\", \"content-available\" : \"1\"}}";  

            //Would need to handle Windows & Android separately
            await hub.SendAppleNativeNotificationAsync(alert, userid);           
        }
    }
}