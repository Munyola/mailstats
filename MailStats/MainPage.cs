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
		class HeaderView : StackLayout
		{
			public HeaderView ()
			{
				SearchBar searchBar = new SearchBar {
					HorizontalOptions = LayoutOptions.FillAndExpand,
					WidthRequest = 10
				};
				searchBar.SetBinding (SearchBar.TextProperty, "SearchBarText");

				var segment = new SegmentedControl.SegmentedControl {
					Children = {
						new SegmentedControlOption { Text = "To Me" },
						new SegmentedControlOption { Text = "From Me" },
					},
					SelectedValue = "To Me",
					VerticalOptions = LayoutOptions.Center

				};

				segment.ValueChanged += (object sender, EventArgs e) => {
					var model = (MainPageViewModel) BindingContext;
					var s = (SegmentedControl.SegmentedControl) sender;
					if (s.SelectedValue == "To Me")
						model.ScoreBoardMaster = model.ToMeScoreboard;
					else
						model.ScoreBoardMaster = model.FromMeScoreboard;
					model.FilterSort ();
				};

				Children.Add (new StackLayout {
					Orientation = StackOrientation.Vertical,
					Children = {
						searchBar,
						new ContentView { Content = segment, Padding = new Thickness (5, 0) }
					}
				});
				Children.Add (new ScoreboardHeader ());
			}
		}

		MainPageViewModel model;

		public MainPage ()
		{
            Title = "Mail Stats";
			model = new MainPageViewModel ();
			BindingContext = model;
			var style = new Style (typeof(Button)) {
				Setters = {
					new Setter {
						Property = Button.HeightRequestProperty,
						Value = Device.OnPlatform(30, 45, 45)
					},
					new Setter {
						Property = Button.BackgroundColorProperty,
						Value = Color.White
					}
				}
			};

			Device.OnPlatform(Android: () => BackgroundColor = Color.White);

			Resources = new ResourceDictionary ();
			Resources.Add (style);

			var template = new DataTemplate (typeof(ScoreboardEntryCell));
			var listView = new ListView {
				ItemTemplate = template,
				RowHeight = 36, 
				HeaderTemplate = new DataTemplate (typeof(ScoreboardHeader)),
				HeightRequest = 10
			};

			listView.SetBinding (ListView.ItemsSourceProperty, "ScoreBoard");
			listView.VerticalOptions = LayoutOptions.FillAndExpand;

			listView.HeaderTemplate = new DataTemplate (typeof (HeaderView));

			listView.Header = model;

			var indicator = new ActivityIndicator ();
			indicator.IsRunning = true;

			var statusLabel = new Label {
				FontSize = 10,
				VerticalOptions = LayoutOptions.CenterAndExpand
			};
			statusLabel.SetBinding (Label.TextProperty, "StatusText");

			var statusLayout = new StackLayout {
				Orientation = StackOrientation.Horizontal,
				BackgroundColor = Color.White,
				Padding = 5,
				Children = {
					indicator,
					statusLabel
				}
			};
			statusLayout.SetBinding (StackLayout.IsVisibleProperty, "IsRunning");

			var grid = new Grid {
				RowDefinitions = {
					new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
					new RowDefinition {	Height = new GridLength(1, GridUnitType.Auto) }
				},

			};

			grid.Children.Add (listView, 0, 1, 0, 2);
			grid.Children.Add (statusLayout, 0, 1, 1, 2);

			Content = grid;
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
