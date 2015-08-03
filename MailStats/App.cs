using System;

using Xamarin.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

using SegmentedControl;

namespace MailStats
{

	public class App : Application
	{
		public static string AppName { get { return "MailStats"; } }
		public static GoogleUser GoogleUser { get; set; }

		public static bool IsLoggedIn { 
			get { 
				if (GoogleUser != null)
					return !string.IsNullOrWhiteSpace (GoogleUser.AccessToken);
				else
					return false;
			} 
		}

		public static Action SuccessfulLoginAction {
			get {
				return new Action (() => { 
					Application.Current.MainPage = new MainPage();
				});
			}
		}
	
		public App ()
		{	
			MainPage = new LoginPage ();
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}


    public class GoogleUser 
    {
        public string Email { get; set; }
        public string AccessToken { get; set; }
    }
}

