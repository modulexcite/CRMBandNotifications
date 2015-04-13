using CoreGraphics;
using CrmBandNotifications.CRM;
using Foundation;
using Microsoft.Band;
using Microsoft.Band.Notifications;
using Microsoft.Band.Tiles;
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using UIKit;

namespace CrmBandNotifications
{
    partial class MainViewController : UIViewController
    {
        private static BandClientManager _manager;
        private static BandClient _client;
        private static readonly NSUuid TileId = new NSUuid("DCBABA9F-12FD-47A5-83A9-E7270A4399BB");

        public MainViewController(IntPtr handle)
            : base(handle)
        {
        }

        public static NSUuid GetTileId()
        {
            return TileId;
        }

        public static BandClient GetClient()
        {
            return _client;
        }

        public static BandClientManager GetManager()
        {
            return _manager;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Do any additional setup after loading the view, typically from a nib.
            string userid = NSUserDefaults.StandardUserDefaults.StringForKey("CrmUserId");
            if (!string.IsNullOrEmpty(userid))
                LoginButton.SetTitle("Logout", UIControlState.Normal);

            _manager = BandClientManager.Instance;

            DismissKeyboardOnBackgroundTap();
        }

        async partial void LoginClick(UIButton sender)
        {
            if (LoginButton.Title(UIControlState.Normal) == "Logout")
            {
                NSUserDefaults.StandardUserDefaults.RemoveObject("CrmUserId");
                LoginButton.SetTitle("Login To CRM", UIControlState.Normal);
                Output("Cleared CRM user id");
                return;
            }

            if (string.IsNullOrEmpty(CrmUrl.Text) || string.IsNullOrEmpty(CrmUsername.Text) ||
                string.IsNullOrEmpty(CrmPassword.Text)) return;

            string url = CrmUrl.Text;
            if (!url.EndsWith("/"))
                url += "/";

            CrmAuthenticationHeader authHeader = null;
            CrmAuth auth = new CrmAuth();
            authHeader = url.Contains("dynamics.com") ?
                auth.GetHeaderOnline(CrmUsername.Text, CrmPassword.Text, url) :
                auth.GetHeaderOnPremise(CrmUsername.Text, CrmPassword.Text, url);

            if (authHeader == null)
            {
                Output("Unable to connect to CRM");
                return;
            }

            string userid = CrmWhoAmI(authHeader, url);
            if (string.IsNullOrEmpty(userid))
            {
                Output("Unable to retrieve user info from CRM");
                return;
            }

            //Save the userid 
            NSUserDefaults.StandardUserDefaults.SetString(userid, "CrmUserId");

            //Register for notifications
            //Moved from AppDelegate.cs - FinishedLaunching
            var settings = UIUserNotificationSettings.GetSettingsForTypes(
                UIUserNotificationType.Alert
                | UIUserNotificationType.Badge
                | UIUserNotificationType.Sound,
                new NSSet());

            UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            LoginButton.SetTitle("Logout", UIControlState.Normal);
            Output("Retrieved CRM user id");
        }

        private static string CrmWhoAmI(CrmAuthenticationHeader authHeader, string url)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Body>");
            xml.Append("<Execute xmlns=\"http://schemas.microsoft.com/xrm/2011/Contracts/Services\">");
            xml.Append("<request i:type=\"c:WhoAmIRequest\" xmlns:b=\"http://schemas.microsoft.com/xrm/2011/Contracts\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:c=\"http://schemas.microsoft.com/crm/2011/Contracts\">");
            xml.Append("<b:Parameters xmlns:d=\"http://schemas.datacontract.org/2004/07/System.Collections.Generic\"/>");
            xml.Append("<b:RequestId i:nil=\"true\"/>");
            xml.Append("<b:RequestName>WhoAmI</b:RequestName>");
            xml.Append("</request>");
            xml.Append("</Execute>");
            xml.Append("</s:Body>");

            XmlDocument xDoc = CrmExecuteSoap.ExecuteSoapRequest(authHeader, xml.ToString(), url);
            if (xDoc == null)
                return null;

            XmlNodeList nodes = xDoc.GetElementsByTagName("b:KeyValuePairOfstringanyType");
            foreach (XmlNode node in nodes)
            {
                if (node.FirstChild.InnerText == "UserId")
                    return node.LastChild.InnerText;
            }

            return null;
        }

        async partial void ConnectToBandClick(UIButton sender)
        {
            if (_client == null)
            {
                // get the client
                _client = _manager.AttachedClients.FirstOrDefault();
                if (_client == null)
                {
                    Output("Failed! No Bands attached.");
                }
                else
                {
                    try
                    {
                        Output("Please wait. Connecting to Band...");
                        await _manager.ConnectTaskAsync(_client);
                        Output("Band connected.");
                    }
                    catch (BandException ex)
                    {
                        Output("Failed to connect to Band:");
                        Output(ex.Message);
                    }
                }
            }
            else
            {
                Output("Please wait. Disconnecting from Band...");
                await _manager.DisconnectTaskAsync(_client);
                Output("Band disconnected.");
            }
        }

        async partial void ToggleAppTileClick(UIButton sender)
        {
            if (_client != null && _client.IsDeviceConnected)
            {
                Output("Creating tile...");

                // the number of tile spaces left
                var capacity = await _client.TileManager.RemainingTileCapacityTaskAsync();
                Output("Remaning tile space: " + capacity);

                // create the tile
                NSError operationError;
                const string tileName = "CRM Notifications";
                var tileIcon = BandIcon.FromUIImage(UIImage.FromBundle("CRM.png"), out operationError);
                var smallIcon = BandIcon.FromUIImage(UIImage.FromBundle("CRMb.png"), out operationError);
                var tile = BandTile.Create(TileId, tileName, tileIcon, smallIcon, out operationError);

                // get the tiles
                try
                {
                    var tiles = await _client.TileManager.GetTilesTaskAsync();
                    if (tiles.Any(x => x.TileId.AsString() == TileId.AsString()))
                    {
                        // a tile exists, so remove it
                        await _client.TileManager.RemoveTileTaskAsync(TileId);
                        Output("Removed tile!");
                    }
                    else
                    {
                        // the tile does not exist, so add it
                        await _client.TileManager.AddTileTaskAsync(tile);
                        Output("Added tile!");
                    }
                }
                catch (BandException ex)
                {
                    Output("Error: " + ex.Message);
                }
            }
            else
                 Output("Band is not connected. Please wait....");
        }

        async partial void SendMessageClick(UIButton sender)
        {
            if (_client != null && _client.IsDeviceConnected)
            {
                Output("Sending notification...");

                try
                {
                    await _client.NotificationManager.SendMessageTaskAsync(TileId, "Hello", "Hello World!", DateTime.Now, true);
                    Output("Sent the message!!");
                }
                catch (BandException ex)
                {
                    Output("Failed to send the message:");
                    Output(ex.Message);
                }
            }
            else
            {
                Output("Band is not connected. Please wait....");
            }
        }

        private void Output(string message)
        {
            OutputText.Text += Environment.NewLine + message;
            CGPoint p = (PointF)OutputText.ContentOffset;
            OutputText.SetContentOffset(p, false);
            OutputText.ScrollRangeToVisible(new NSRange(OutputText.Text.Length, 0));
        }

        protected void DismissKeyboardOnBackgroundTap()
        {
            // Add gesture recognizer to hide keyboard
            var tap = new UITapGestureRecognizer { CancelsTouchesInView = false };
            tap.AddTarget(() => View.EndEditing(true));
            View.AddGestureRecognizer(tap);
        }
    }
}
