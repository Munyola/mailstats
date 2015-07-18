using System;

using Xamarin.Forms;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MailStats
{

	public class MainPageViewModel : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		bool isRunning;
		public bool IsRunning {
			get {
				return isRunning;
			}
			set {
				isRunning = value;
				OnPropertyChanged ("IsRunning");
			}
		}

		public void OnPropertyChanged (string propertyName)
		{
			PropertyChanged?.Invoke (this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class App : Application
	{
		Label label;
		MainPageViewModel model;

		public App ()
		{
			model = new MainPageViewModel ();

			var button = new Button {
				Text = "Fetch email"
			};

			label = new Label {
				Text = "0 emails",
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.CenterAndExpand
			};
			
			var indicator = new ActivityIndicator ();
			indicator.SetBinding (ActivityIndicator.IsRunningProperty, "IsRunning");
				
			button.Clicked += OnButtonClicked;

			// The root page of your application
			MainPage = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						button,
						label,
						indicator
					}
				}
			};

			MainPage.BindingContext = model;
		}

		async void OnButtonClicked(object sender, EventArgs e)
		{
			try {
				model.IsRunning = true;
				await Task.Run (() => {
					var emailpassword = System.IO.File.ReadAllText ("/tmp/gmail.txt").Split (',');
					var email = emailpassword [0];
					var password = emailpassword [1];
					MainClass.FetchNewEmails (email, password, 30);
					MainClass.CalculateStatistics (email, 30);
				});
			} catch (Exception ex) {
				Console.WriteLine (ex);
			} finally {
				model.IsRunning = false;
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

