using Foundation;
using Microsoft.Band;
using Microsoft.Band.Notifications;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace CrmBandNotifications
{
    [Foundation.Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        //TODO: Populate based on your Azure instance
        private const string ApplicationUrl = "Azure Mobile Services - URL";
        private const string ApplicationKey = "Azure Mobile Services - Application Key";
        public string DeviceToken { get; set; }
        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            return true;
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            DeviceToken = deviceToken.Description;
            DeviceToken = DeviceToken.Trim('<', '>').Replace(" ", "");

            MobileServiceClient client = new MobileServiceClient(ApplicationUrl, ApplicationKey);

            string userid = NSUserDefaults.StandardUserDefaults.StringForKey("CrmUserId");
            if (string.IsNullOrEmpty(userid)) return;

            //Tags could be expanded to handle multiple different scenarios
            IEnumerable<string> tags = new List<string>() { userid, "All Users" };
            var push = client.GetPush();
            push.RegisterNativeAsync(DeviceToken, tags);
        }
        async public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            if (null != userInfo && userInfo.ContainsKey(new NSString("aps")))
            {
                NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;

                string alert = string.Empty;
                if (aps.ContainsKey(new NSString("alert")))
                {
                    var nsString = aps[new NSString("alert")] as NSString;
                    if (nsString != null)
                        alert = nsString.ToString();
                }

                if (!string.IsNullOrEmpty(alert))
                {
                    //Show a pop-up if the application is open
                    UIAlertView avAlert = new UIAlertView("CRM Item", alert, null, "OK", null);
                    avAlert.Show();

                    BandClient client = MainViewController.GetClient();

                    //Connect to Band if not already connected
                    if (client == null)
                    {
                        try
                        {
                            BandClientManager manager = MainViewController.GetManager();
                            client = manager.AttachedClients.FirstOrDefault();
                            if (client == null) return;

                            client = MainViewController.GetClient();
                        }
                        catch (BandException) { }
                    }

                    if (client == null) return;

                    //Send to Band
                    await client.NotificationManager.SendMessageTaskAsync(MainViewController.GetTileId(), "CRM Item",
                        alert, DateTime.Now, true);
                }
            }
        }
    }
}

