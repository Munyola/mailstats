using System;
using Xamarin.Forms;
using SimpleAuth;
using SimpleAuth.Providers;
using System.Threading.Tasks;

using Xamarin;
using System.Collections.Generic;

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

				var traits = new Dictionary<string, string> {
					{Insights.Traits.Email, profile.Email},
					{Insights.Traits.Gender, profile.Gender},
					{Insights.Traits.FirstName, profile.GivenName},
					{Insights.Traits.LastName, profile.FamilyName},
					{Insights.Traits.Avatar, profile.Picture}
				};
				Insights.Identify(profile.Id, traits);

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

