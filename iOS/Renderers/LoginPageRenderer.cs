using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Auth;
using MailStats;
using MailStats.iOS;

[assembly: ExportRendererAttribute (typeof (LoginPage), typeof (LoginPageRenderer))]
namespace MailStats.iOS
{
	public class LoginPageRenderer : PageRenderer
	{
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);

			Console.WriteLine ("In login page renderer....");

			var accounts = AccountStore.Create ().FindAccountsForService (App.AppName);
			//var account = accounts.FirstOrDefault ();

			var auth = new OAuth2Authenticator (
				MailStats.Constants.ClientId,
				MailStats.Constants.ClientSecret,
				Constants.Scope,
				new Uri (Constants.AuthorizeUrl), // the auth URL for the service
				new Uri (Constants.RedirectUrl), // redirect URL
				new Uri (Constants.AccessTokenUrl) // access token URL
			);
			auth.AllowCancel = true;
			auth.ShowUIErrors = false;
			auth.ClearCookiesBeforeLogin = false;

			auth.Completed += (sender, e) => {

				if (e.IsAuthenticated) {
					var user = new GoogleUser();
					user.AccessToken = e.Account.Properties["access_token"];
					user.RefreshToken = e.Account.Properties["refresh_token"];
					App.GoogleUser = user;

					// We presented the UI, so it's up to us to dimiss it on iOS.
					App.SuccessfulLoginAction.Invoke();
				} else {
					// FIXME: what do we do?
				}
			};

			PresentViewController (auth.GetUI (), true, null);
		}
	}

}

