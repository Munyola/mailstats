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

		static Task<List<EmailScoreEntry>> SortEmails(List<EmailScoreEntry> emails, string property)
		{
            return Task.Run<List<EmailScoreEntry>>(() =>
                {
                    var propertyInfo = typeof(EmailScoreEntry).GetProperty(property);    
                    return emails.OrderBy(x => propertyInfo.GetValue(x, null)).ToList();
                });
		}

		public async void FilterSort ()
		{
			if (ScoreBoardMaster == null)
				return;
			
			var list = await SortEmails (ScoreBoardMaster, CurrentSort);

			if (searchBarText?.Length > 0) {
				var lowercase = searchBarText.ToLower ();
				list = list.Where (x => x.Email.ToLower ().Contains (lowercase)).ToList ();
			}

			if (!CurrentSortAscending)
				list.Reverse ();

			ScoreBoard = list;			
		}
	}
	
}
