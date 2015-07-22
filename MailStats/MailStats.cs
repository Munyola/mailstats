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

		List<EmailData> scoreBoard;
		public List<EmailData> ScoreBoard {
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

	public class ScoreboardHeader : Grid
	{
		public Label email, count, mean;

		public ScoreboardHeader ()
		{

			email = new Label {
				Text = "Person"
			};

			count = new Label {
				Text = "Count",
				XAlign = TextAlignment.Center
			};

			mean = new Label {
				Text = "Reply time",
				XAlign = TextAlignment.Center
			};

			Padding = new Thickness (10);
			ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (4, GridUnitType.Star) });
			ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (1, GridUnitType.Star) });
			ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (2, GridUnitType.Star) });

			Children.Add (email, 0, 0);
			Children.Add (count, 1, 0);
			Children.Add (mean, 2, 0);
		}
	}


	public class ScoreboardEntryCell : ViewCell
	{
		
		protected Label email, count, mean, min, max;

		public ScoreboardEntryCell ()
		{
			email = new Label ();
			email.Text = "Person";
			email.SetBinding (Label.TextProperty, "Name");

			count = new Label ();
			count.Text = "# replies";
			count.XAlign = TextAlignment.End;
			count.SetBinding (Label.TextProperty, "ReplyTimesCount");

			mean = new Label ();
			mean.Text = "Avg. mins";
			mean.XAlign = TextAlignment.End;
			mean.SetBinding (Label.TextProperty, "ReplyTimesAverageString");

			var grid = new Grid {
				Padding = new Thickness (10),
				ColumnDefinitions = {
					new ColumnDefinition { Width = new GridLength (4, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength (1, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength (2, GridUnitType.Star) }
				}
			};

			grid.Children.Add (email, 0, 0);
			grid.Children.Add (count, 1, 0);
			grid.Children.Add (mean, 2, 0);

			View = grid;
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
				ItemTemplate = template,
				HeaderTemplate = new DataTemplate (typeof(ScoreboardHeader))
			};

			listView.SetBinding (ListView.ItemsSourceProperty, "ScoreBoard");

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.Center,
				Children = {
					button,
					indicator,
					new ScoreboardHeader(),
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
						var emailData = MainClass.CalculateStatistics (email, 30);
						model.ScoreBoard = emailData.Where(X => X.Value.ReplyTimesMinutes.Count > 0).Select(X => X.Value).Where(X => X.ReplyTimesCount > 2).OrderBy(X => X.ReplyTimesAverage).ToList();
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

