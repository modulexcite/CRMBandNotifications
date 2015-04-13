// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace CrmBandNotifications
{
	[Register ("MainViewController")]
	partial class MainViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UITextField CrmPassword { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UITextField CrmUrl { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UITextField CrmUsername { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIButton LoginButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UITextView OutputText { get; set; }

		[Action ("ConnectToBandClick:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void ConnectToBandClick (UIButton sender);

		[Action ("LoginClick:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void LoginClick (UIButton sender);

		[Action ("SendMessageClick:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void SendMessageClick (UIButton sender);

		[Action ("ToggleAppTileClick:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void ToggleAppTileClick (UIButton sender);

		void ReleaseDesignerOutlets ()
		{
			if (CrmPassword != null) {
				CrmPassword.Dispose ();
				CrmPassword = null;
			}
			if (CrmUrl != null) {
				CrmUrl.Dispose ();
				CrmUrl = null;
			}
			if (CrmUsername != null) {
				CrmUsername.Dispose ();
				CrmUsername = null;
			}
			if (LoginButton != null) {
				LoginButton.Dispose ();
				LoginButton = null;
			}
			if (OutputText != null) {
				OutputText.Dispose ();
				OutputText = null;
			}
		}
	}
}
