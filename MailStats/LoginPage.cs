using System;
using Xamarin.Forms;
using SimpleAuth;
using SimpleAuth.Providers;
using System.Threading.Tasks;

namespace MailStats
{
	public class LoginPage : ContentPage
	{
		OAuthApi api;

		protected override async void OnAppearing ()
		{
			base.OnAppearing ();

			try
			{
				var account = await api.Authenticate();

				var google = (GoogleApi) api;
				var profile = await google.GetUserInfo();
				App.GoogleUser = new GoogleUser();
				App.GoogleUser.Email = profile.Email;
				App.GoogleUser.AccessToken = api.CurrentOAuthAccount.Token;
				
				App.SuccessfulLoginAction.Invoke ();
			}
			catch (TaskCanceledException ex)
			{
				Console.WriteLine("Canceled");
			}
		}

		public LoginPage ()
		{
			api = new GoogleApi("google", Constants.ClientId, Constants.ClientSecret)
			{
				Scopes = Constants.Scopes
			};
		}
	}

}

