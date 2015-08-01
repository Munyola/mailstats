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
	public class MainPageViewModel : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		public string CurrentSort = "MeanReplyTime";
		public bool CurrentSortAscending = true;

		string searchBarText;
		public string SearchBarText {
			get {
				return searchBarText;
			}
			set {
				searchBarText = value;
				FilterSort ();
				OnPropertyChanged ();
			}
		}

		bool isRunning;
		public bool IsRunning {
			get {
				return isRunning;
			}
			set {
				isRunning = value;
				OnPropertyChanged ();
			}
		}

		string statusText;
		public string StatusText {
			get {
				return statusText;
			}
			set {
				statusText = value;
				OnPropertyChanged ();
			}
		}

		// The computed scoreboards for emails to and from me; we swap
		// the displayed scoreboard between these two when the user 
		// toggles the view.
		public List<EmailScoreEntry> ToMeScoreboard {get; set;}
		public List<EmailScoreEntry> FromMeScoreboard {get; set;}

		public List<EmailScoreEntry> ScoreBoardMaster {get; set;}

		// The filtered/sorted version of the scoreboard
		List<EmailScoreEntry> scoreBoard;
		public List<EmailScoreEntry> ScoreBoard {
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

		static List<EmailScoreEntry> SortEmails(List<EmailScoreEntry> emails, string property)
		{
			var propertyInfo = typeof(EmailScoreEntry).GetProperty(property);    
			return emails.OrderBy(x => propertyInfo.GetValue(x, null)).ToList();
		}

		public void FilterSort ()
		{
			if (ScoreBoardMaster == null)
				return;
			
			var list = SortEmails (ScoreBoardMaster, CurrentSort);

			if (searchBarText?.Length > 0) {
				var lowercase = searchBarText.ToLower ();
				list = list.Where (x => x.Email.ToLower ().Contains (lowercase)).ToList ();
			}

			if (!CurrentSortAscending)
				list.Reverse ();

			ScoreBoard = list;			
		}
	}

	public class ScoreboardHeader : Grid
	{
		public Button email, count, mean;

		public ScoreboardHeader ()
		{
			email = new Button {
				Text = "Person",
				FontAttributes = FontAttributes.Bold
			};

			count = new Button {
				Text = "Count",
				FontAttributes = FontAttributes.Bold,
			};

			mean = new Button {
				Text = "Reply time",
				FontAttributes = FontAttributes.Bold,
			};

			count.Clicked += (object sender, EventArgs e) => {
				var model = (MainPageViewModel) BindingContext;
				model.CurrentSort = "EmailCount";
				model.CurrentSortAscending = ! model.CurrentSortAscending;
				model.FilterSort ();
			};

			email.Clicked += (object sender, EventArgs e) => {
				var model = (MainPageViewModel) BindingContext;
				model.CurrentSort = "Name";
				model.CurrentSortAscending = ! model.CurrentSortAscending;
				model.FilterSort ();
			};

			mean.Clicked += (object sender, EventArgs e) => {
				var model = (MainPageViewModel) BindingContext;
				model.CurrentSort = "MeanReplyTime";
				model.CurrentSortAscending = ! model.CurrentSortAscending;
				model.FilterSort ();
			};

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
		
		protected Label name, email, count, mean, min, max;

		public ScoreboardEntryCell ()
		{
			var fontSize = 12;

			name = new Label ();
			name.FontSize = fontSize;
			name.SetBinding (Label.TextProperty, "Name");

			// FIXME: Can't figure out how to make this light grey.
			email = new Label ();
			email.FontSize = fontSize - 2;
			email.SetBinding (Label.TextProperty, "EmailAddress");

			count = new Label ();
			count.FontSize = fontSize;
			count.XAlign = TextAlignment.End;

			count.SetBinding (Label.TextProperty, "EmailCount");

			mean = new Label ();
			mean.FontSize = fontSize;
			mean.XAlign = TextAlignment.End;
			mean.SetBinding (Label.TextProperty, "MeanReplyTimeString");

			var grid = new Grid {
				Padding = new Thickness (5),
				ColumnDefinitions = {
					new ColumnDefinition { Width = new GridLength (4, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength (1, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength (2, GridUnitType.Star) }
				}
			};

			var stack = new StackLayout {
				Spacing = 0, 
				Children = {
					name,
					email
				}
			};

			grid.Children.Add (stack, 0, 0);
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

			var indicator = new ActivityIndicator ();
			indicator.SetBinding (ActivityIndicator.IsRunningProperty, "IsRunning");

			var template = new DataTemplate (typeof(ScoreboardEntryCell));
			var listView = new ListView {
				ItemTemplate = template,
				RowHeight = 36, 
				HeaderTemplate = new DataTemplate (typeof(ScoreboardHeader))
			};

			listView.SetBinding (ListView.ItemsSourceProperty, "ScoreBoard");

			SearchBar searchBar = new SearchBar ();
			searchBar.SetBinding (SearchBar.TextProperty, "SearchBarText");


			var segment = new SegmentedControl.SegmentedControl {
				Children = {
					new SegmentedControlOption { Text = "To Me" },
					new SegmentedControlOption { Text = "From Me" },
				},
				SelectedIndex = 0
			};

			segment.ValueChanged += (object sender, EventArgs e) => {
				var s = (SegmentedControl.SegmentedControl) sender;
				if (s.SelectedValue == "To Me")
					model.ScoreBoardMaster = model.ToMeScoreboard;
				else
					model.ScoreBoardMaster = model.FromMeScoreboard;
				model.FilterSort ();
			};

			var statusLabel = new Label {
				FontSize = 10,
				VerticalOptions = LayoutOptions.CenterAndExpand
			};
			statusLabel.SetBinding (Label.TextProperty, "StatusText");

			var topLayout = new StackLayout {
				Orientation = StackOrientation.Horizontal,
				Children = {
					segment,
					indicator,
					statusLabel
				}
			};

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.Center,
				Children = {
					topLayout,
					searchBar,
					new ScoreboardHeader(),
					listView
				}
			};

			// Accommodate iPhone status header
			this.Padding = new Thickness(0, 
				Device.OnPlatform(20, 0, 0), // iOS, Android, WinPhone
				0, 0);
		}

		protected override void OnAppearing ()
		{
			base.OnAppearing ();
			Refresh ();
		}

		Task syncingTask;

		async void Refresh ()
		{
			try {
				model.IsRunning = true;
				if (syncingTask == null || syncingTask.IsCompleted == true) 
					syncingTask = Task.Run (async () => {
						if (MailFetch.NumEmails () == 0) {
							model.StatusText = "Fetching 7 days of email...";
							await MailFetch.FetchNewEmails (Constants.InitialFetchDaysAgo);
						}

						model.StatusText = "Computing leaderboard...";
						await RefreshTable();
						model.StatusText = "Fetching 6 months of email..."; // FIXME: not really six months if we've already fetched...
						await MailFetch.FetchNewEmails (Constants.DaysAgo);
						model.StatusText = "Recomputing leaderboard..."; // FIXME no need to recompute if we didn't fetch anyhting
						await RefreshTable ();
						model.StatusText = "";
					});

				await syncingTask;
			} catch (Exception ex) {
				Console.WriteLine (ex);
			} finally {
				model.IsRunning = false;
			}
		}

		async Task RefreshTable()
		{
			var emailData = CalcStats.CalculateStatistics (Constants.DaysAgo);
			model.ToMeScoreboard = 
				emailData.Where (x => x.Value.ReplyTimesCount > 2).Select (x => x.Value).OrderBy (x => x.ReplyTimesAverage).
				Select(x => new EmailScoreEntry(x, false)).ToList ();
			model.FromMeScoreboard = 
				emailData.Where (x => x.Value.MyReplyTimesCount > 2).Select (X => X.Value).OrderBy (X => X.MyReplyTimesAverage).
				Select(x => new EmailScoreEntry(x, true)).ToList ();

			model.ScoreBoardMaster = model.ToMeScoreboard;
						
			model.FilterSort ();
		}
	}

	public class GoogleUser 
	{
		public string Email { get; set; }
		public string AccessToken { get; set; }
	}

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
}

