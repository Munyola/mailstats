using System;

using SQLite;
using Xamarin.Forms;

namespace MailStats
{
	public class App : Application
	{
		Label label;

		public App ()
		{
			var button = new Button {
				Text = "Fetch email"
			};

			label = new Label {
				Text = "0 emails",
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.CenterAndExpand
			};

			button.Clicked += OnButtonClicked;

			// The root page of your application
			MainPage = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						button,
						label
					}
				}
			};
		}

		void OnButtonClicked(object sender, EventArgs e)
		{
			label.Text = "1";
			try {
				var db = new SQLiteConnection ("email.db");
				var emailpassword = System.IO.File.ReadAllText("/tmp/gmail.txt").Split(',');
				var email = emailpassword[0];
				var password = emailpassword[1];
					MainClass.FetchNewEmails (email, password, 30, db);
				MainClass.CalculateStatistics (db, email, 30);
			}  catch (Exception xx) {
				Console.WriteLine ("Exception: {0}", xx);
			}
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
}

