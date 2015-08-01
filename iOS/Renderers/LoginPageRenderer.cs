using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Auth;
using MailStats;
using MailStats.iOS;
using System.Linq;

// FIXME: Why is all this in a custom renderer?

[assembly: ExportRendererAttribute (typeof (LoginPage), typeof (LoginPageRenderer))]
namespace MailStats.iOS
{
	public class LoginPageRenderer : PageRenderer
	{
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);

			var acctStore = AccountStore.Create ();
			var accounts = acctStore.FindAccountsForService (App.AppName);
			var account = accounts.FirstOrDefault ();

			if (account == null) {
				var auth = new OAuth2Authenticator (
					           Constants.ClientId,
					           Constants.ClientSecret,
					           Constants.Scope,
					           new Uri (Constants.AuthorizeUrl),
					           new Uri (Constants.RedirectUrl),
					           new Uri (Constants.AccessTokenUrl));
				auth.AllowCancel = false;
				auth.ShowUIErrors = false;
				auth.ClearCookiesBeforeLogin = false;

				auth.Completed += (sender, e) => {

					if (e.IsAuthenticated) {

						// Get the user's email address
						var profile = GoogleApiService.Instance.GetUserProfile (e.Account.Properties["access_token"]);
						e.Account.Username = profile.Email;
						AccountStore.Create ().Save (e.Account, App.AppName);

						var user = new GoogleUser ();
						user.AccessToken = e.Account.Properties ["access_token"];
						user.RefreshToken = e.Account.Properties ["refresh_token"];
						user.Email = profile.Email;
						App.GoogleUser = user;

						Console.WriteLine("access_token: '{0}', email: '{1}'", user.AccessToken, user.Email);

						// Transition to the main app
						App.SuccessfulLoginAction.Invoke ();
					} else {
						// FIXME: what do we do?
					}
				};

				PresentViewController (auth.GetUI (), true, null);
			} else {

				account.Properties["access_token"] = GoogleApiService.Instance.GetNewAuthToken(account.Properties["refresh_token"]);
				acctStore.Save (account, App.AppName);

				var user = new GoogleUser ();
				user.AccessToken = account.Properties ["access_token"];
				user.RefreshToken = account.Properties ["refresh_token"];
				user.Email = account.Username;
				App.GoogleUser = user;

				Console.WriteLine("access_token: '{0}', email: '{1}'", user.AccessToken, user.Email);

				App.SuccessfulLoginAction.Invoke ();
			}
		}
	}

}

