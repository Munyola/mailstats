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

	public class MainPage : ContentPage
	{
		MainPageViewModel model;

		public MainPage ()
		{
            Title = "Mail Stats";
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
			listView.VerticalOptions = LayoutOptions.FillAndExpand;

			SearchBar searchBar = new SearchBar ();
			searchBar.SetBinding (SearchBar.TextProperty, "SearchBarText");

			// FIXME: need the Android renderer
			var segment = new SegmentedControl.SegmentedControl {
                HorizontalOptions = LayoutOptions.FillAndExpand,
				Children = {
					new SegmentedControlOption { Text = "To Me" },
					new SegmentedControlOption { Text = "From Me" },
				},
				SelectedValue = "To Me"
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
				Padding = 5,
				Children = {
					segment,
					indicator,
					statusLabel
				}
			};

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.FillAndExpand,
				Children = {
					topLayout,
					searchBar,
					new ScoreboardHeader(),
					listView
				}
			};

			// Accommodate iPhone status header
			/*this.Padding = new Thickness(0, 
				Device.OnPlatform(20, 0, 0), // iOS, Android, WinPhone
				0, 0);*/
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
							await MailFetch.FetchNewEmails (Constants.InitialFetchDaysAgo, (percent, emailsFetched, totalEmails) => {
								model.StatusText = $"Fetched {emailsFetched}/{totalEmails} emails - {percent}%";});
						}

						model.StatusText = "Computing leaderboard...";
						await RefreshTable();

						model.StatusText = "Fetching " + Constants.DaysAgo + " days of email..."; // FIXME: not really six months if we've already fetched...
						var fetched = 0;
						await MailFetch.FetchNewEmails (Constants.DaysAgo, (percent, emailsFetched, totalEmails) => {
							fetched ++;
							model.StatusText = $"Fetched {emailsFetched}/{totalEmails} emails - {percent}%";
							Console.WriteLine ("Fetched {0}/{1} ({2}%) email headers", emailsFetched, totalEmails, percent); 
						});

						if (fetched > 0) {
							model.StatusText = "Recomputing leaderboard...";
							await RefreshTable ();
						}
						//model.StatusText = String.Format("{0} emails.", MailFetch.NumEmails ().ToString("N0"));
						model.StatusText = "";
					});

				await syncingTask;
			} catch (Exception ex) {
				Xamarin.Insights.Report (ex);
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
				emailData.Where (x => x.Value.MyReplyTimesCount > 2).Select (x => x.Value).OrderBy (x => x.MyReplyTimesAverage).
				Select(x => new EmailScoreEntry(x, true)).ToList ();

			model.ScoreBoardMaster = model.ToMeScoreboard;
						
			model.FilterSort ();
		}
	}
	
}
