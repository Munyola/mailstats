using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Auth;

namespace MailStats.iOS
{
	[assembly: ExportRendererAttribute (typeof (LoginPage), typeof (LoginPageRenderer))]
	public class LoginPageRenderer : PageRenderer
	{
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);

			var auth = new OAuth2Authenticator (
				// FIXME What do I put in here?
				clientId: "", // your OAuth2 client id
				scope: "", // the scopes for the particular API you're accessing, delimited by "+" symbols
				authorizeUrl: new Uri (""), // the auth URL for the service
				redirectUrl: new Uri ("")); // the redirect URL for the service

			auth.Completed += (sender, eventArgs) => {
				// We presented the UI, so it's up to us to dimiss it on iOS.
				App.SuccessfulLoginAction.Invoke();

				if (eventArgs.IsAuthenticated) {
					// Use eventArgs.Account to do wonderful things
					App.SaveToken(eventArgs.Account.Properties["access_token"]);
				} else {
					// The user cancelled
				}
			};

			PresentViewController (auth.GetUI (), true, null);
		}
	}

}

