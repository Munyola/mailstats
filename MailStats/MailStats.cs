using System;

using Xamarin.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

using SimpleAuth;
using SimpleAuth.OAuth;

using Xamarin.Auth;

namespace MailStats
{

	public class MainPageViewModel : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		public string CurrentSort = "ReplyTimesAverage";
		public bool CurrentSortAscending = true;

		static List<EmailData> SortEmails(List<EmailData> emails, string property)
		{
			var propertyInfo = typeof(EmailData).GetProperty(property);    
			return emails.OrderBy(x => propertyInfo.GetValue(x, null)).ToList();
		}
			
		public void FilterSort ()
		{
			List<EmailData> list = ScoreBoardMaster;

			list = SortEmails (ScoreBoardMaster, CurrentSort);

			if (searchBarText?.Length > 0) {
				var lowercase = searchBarText.ToLower ();
				list = list.Where (x => x.Email.ToLower ().Contains (lowercase)).ToList ();
			}

			if (!CurrentSortAscending)
				list.Reverse ();

			ScoreBoard = list;			
		}

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

		List<EmailData> scoreBoardMaster;
		public List<EmailData> ScoreBoardMaster {
			get {
				return scoreBoardMaster;
			}
			set {
				scoreBoardMaster = value;
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
				model.CurrentSort = "ReplyTimesCount";
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
				model.CurrentSort = "ReplyTimesAverage";
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

			count.SetBinding (Label.TextProperty, "ReplyTimesCount");

			mean = new Label ();
			mean.FontSize = fontSize;
			mean.XAlign = TextAlignment.End;
			mean.SetBinding (Label.TextProperty, "ReplyTimesAverageString");

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

	public class BaseContentPage : ContentPage
	{
		protected override void OnAppearing ()
		{
			base.OnAppearing ();

			if (!App.IsLoggedIn) {
				Navigation.PushModalAsync(new LoginPage());
			}
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

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.Center,
				Children = {
					searchBar,
					new ScoreboardHeader(),
					listView,
					indicator
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
			Refresh();
		}

		Task syncingTask;

		async void Refresh()
		{
			try {
				model.IsRunning = true;
				if (syncingTask == null || syncingTask.IsCompleted == true) 
					syncingTask = Task.Run (async () => {
						await Task.WhenAll (RefreshTable(), MainClass.FetchNewEmails (180));
						await RefreshTable ();
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
			var emailData = MainClass.CalculateStatistics (180);
			model.ScoreBoardMaster = 
				emailData.Where (X => X.Value.ReplyTimesMinutes.Count > 0).Select (X => X.Value).Where (X => X.ReplyTimesCount > 2).OrderBy (X => X.ReplyTimesAverage).ToList ();
			model.FilterSort ();
		}
	}

	public class LoginPage : ContentPage
	{
//		public LoginPage ()
//		{
//			OAuthApi api;
//
//			api = new OAuthApi("google", new OAuthAuthenticator(
//				Constants.AuthorizeUrl,
//				Constants.AccessTokenUrl,
//				Constants.RedirectUrl,
//				Constants.ClientId,
//				Constants.ClientSecret));
//			var button = new Button
//			{
//				Text = "Login",
//			};
//
//			button.Clicked += async (sender, args) =>
//			{
//				try
//				{
//					var account = await api.Authenticate();
//					Console.WriteLine(account.Identifier);
//					App.SuccessfulLoginAction.Invoke ();
//				}
//				catch (TaskCanceledException ex)
//				{
//					Console.WriteLine("Canceled");
//				}
//			};
//
//			// The root page of your application
//			Content = new StackLayout {
//				VerticalOptions = LayoutOptions.Center,
//				Children = {
//					button
//				}
//			};
//		}
	}

	public class GoogleUser 
	{
		public string Email { get; set; }
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
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
			//_NavPage = new NavigationPage (new LoginPage ());
			//MainPage = _NavPage;
			MainPage = new LoginPage();
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

