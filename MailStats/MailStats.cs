using System;

using Xamarin.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace MailStats
{

	public class MainPageViewModel : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		bool isRunning;
		string statusLabelText;
		public string StatusLabelText {
			get {
				return statusLabelText;
			}
			set {
				statusLabelText = value;
				OnPropertyChanged ();
			}
		}
		public bool IsRunning {
			get {
				return isRunning;
			}
			set {
				isRunning = value;
				OnPropertyChanged ();
			}
		}

		List<ScoreboardEntry> scoreBoard;
		public List<ScoreboardEntry> ScoreBoard {
			get {
				return scoreBoard;
			}
			set {
				scoreBoard = value;
				OnPropertyChanged ();
			}
		}

		public void OnPropertyChanged ([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke (this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ScoreboardEntryCell : ViewCell
	{
		
		public ScoreboardEntryCell ()
		{
			Label email, count, mean, min, max;

			email = new Label ();
			email.SetBinding (Label.TextProperty, "Email");

			count = new Label ();
			count.SetBinding (Label.TextProperty, "TheirReplyCount");

			mean = new Label ();
			mean.SetBinding (Label.TextProperty, "TheirMeanReply");

			min = new Label ();
			min.SetBinding (Label.TextProperty, "TheirMinReply");

			max = new Label ();
			max.SetBinding (Label.TextProperty, "TheirMaxReply");

			View = new StackLayout {
				Orientation = StackOrientation.Horizontal,
				Children = {
					email, count, mean, min, max
				}
			};
		}
	}

	public class MainPage : ContentPage
	{
		MainPageViewModel model;

		public MainPage ()
		{
			model = new MainPageViewModel ();
			BindingContext = model;

			var button = new Button {
				Text = "Fetch email"
			};

			var label = new Label {
				Text = "0 emails",
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.CenterAndExpand
			};
			label.SetBinding (Label.TextProperty, "StatusLabelText");

			var indicator = new ActivityIndicator ();
			indicator.SetBinding (ActivityIndicator.IsRunningProperty, "IsRunning");

			var template = new DataTemplate (typeof(ScoreboardEntryCell));
			var listView = new ListView {
				ItemTemplate = template
			};

			listView.SetBinding (ListView.ItemsSourceProperty, "ScoreBoard");

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.Center,
				Children = {
					button,
					label,
					indicator,
					listView
				}
			};

			button.Clicked += OnButtonClicked;
		}

		Task syncingTask;

		async void OnButtonClicked(object sender, EventArgs e)
		{
			try {
				model.IsRunning = true;
				if (syncingTask == null || syncingTask.IsCompleted == true) 
					syncingTask = Task.Run (() => {
						var emailpassword = System.IO.File.ReadAllText ("/tmp/gmail.txt").Trim().Split (',');
						var email = emailpassword [0];
						var password = emailpassword [1];
						model.StatusLabelText = "Fetching new emails...";
						MainClass.FetchNewEmails (email, password, 30);
						model.StatusLabelText = "Calculating statistics...";
						var d = MainClass.CalculateStatistics (email, 30);
						model.ScoreBoard = d.Select(X => X.Value).ToList();
						model.StatusLabelText = "Done!";
					});

				await syncingTask;
			} catch (Exception ex) {
				model.StatusLabelText = "An error occurred";
				Console.WriteLine (ex);
			} finally {
				model.IsRunning = false;
			}
		}
	}

	public class App : Application
	{
		public App ()
		{
			MainPage = new MailStats.MainPage ();
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

