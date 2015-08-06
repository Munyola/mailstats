using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Xamarin;

namespace MailStats.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Insights.Initialize("00e2e3fb179d9c7144f599bc619cfb914081d2ea");
            UINavigationBar.Appearance.BarTintColor = UIColor.FromRGB(43, 132, 211); //bar background
            UINavigationBar.Appearance.TintColor = UIColor.White; //Tint color of button items
            UINavigationBar.Appearance.SetTitleTextAttributes(new UITextAttributes()
                {
                    Font = UIFont.FromName("HelveticaNeue-Light", (nfloat)20f),
                    TextColor = UIColor.White
                });
			global::Xamarin.Forms.Forms.Init ();
			SimpleAuth.OnePassword.Activate();

			// Code for starting up the Xamarin Test Cloud Agent
			#if ENABLE_TEST_CLOUD
			//Xamarin.Calabash.Start();
			#endif

			LoadApplication (new App ());

			return base.FinishedLaunching (app, options);
		}
	}
}

