using System;
using System.Collections.Generic;
using System.Linq;

using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;

using SQLite;
using System.IO;
using System.Threading.Tasks;

namespace MailStats
{

	class Locations
	{
		public static string BaseDir = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Personal)).ToString();
		public static readonly string LibDir = Path.Combine(BaseDir, "Library/");
	}

	class Database : SQLiteConnection
	{
		public static Database Main { get; } = new Database ();

		public Database () : base (Path.Combine(Locations.LibDir, "emails.db"), true)
		{
			CreateTable<Email>();
			CreateTable<SyncState>();
		}

		public int InsertOrReplaceAll (IEnumerable<Object> items)
		{
			return this.InsertAll(items, "OR REPLACE");
		}
	}

	class SyncState
	{
		[PrimaryKey]
		public string EmailAddress { get; set; }
		public DateTime DownloadStart { get; set; }
		public DateTime DownloadEnd { get; set; }
	}

	public class EmailData
	{
		public string Email {get;set;}
		public string Name => ParseName (Email);
		public string EmailAddress => ParseEmailAddress (Email);

		// Their responses to emails from me
		public Dictionary<string,int> ReplyTimesMinutes { get; set; } = new Dictionary<string,int>();
		public double ReplyTimesAverage => ReplyTimesMinutes.Count == 0 ? 0 : ReplyTimesMinutes.Values.Average();
		public string ReplyTimesAverageString => MinutesToString (ReplyTimesAverage);
		public int ReplyTimesMin => ReplyTimesMinutes.Count == 0 ? 0 : ReplyTimesMinutes.Values.Min();
		public int ReplyTimesMax => ReplyTimesMinutes.Count == 0 ? 0 : ReplyTimesMinutes.Values.Max();
		public int ReplyTimesCount => ReplyTimesMinutes.Count;

		// My responses to emails from them
		public Dictionary<string,int> MyReplyTimes { get; set; } = new Dictionary<string,int>();
		public double MyReplyTimesAverage => MyReplyTimes.Count == 0 ? 0 : MyReplyTimes.Values.Average();
		public string MyReplyTimesAverageString => MinutesToString (MyReplyTimesAverage);
		public int MyReplyTimesMin => MyReplyTimes.Count == 0 ? 0 : MyReplyTimes.Values.Min();
		public int MyReplyTimesMax => MyReplyTimes.Count == 0 ? 0 : MyReplyTimes.Values.Max();
		public int MyReplyTimesCount => MyReplyTimes.Count;

		// Their responses to emails from other people
		public Dictionary<string,int> OtherReplyTimesMinutes { get; set; } = new Dictionary<string,int>();
		public double OtherReplyTimesAverage => OtherReplyTimesMinutes.Count == 0 ? 0 : OtherReplyTimesMinutes.Values.Average();
		public string OtherReplyTimesAverageString => MinutesToString (OtherReplyTimesAverage);
		public int OtherReplyTimesMin => OtherReplyTimesMinutes.Count == 0 ? 0 : OtherReplyTimesMinutes.Values.Min();
		public int OtherReplyTimesMax => OtherReplyTimesMinutes.Count == 0 ? 0 : OtherReplyTimesMinutes.Values.Max();
		public int OtherReplyTimesCount => OtherReplyTimesMinutes.Count;

		public static string ParseName(string email)
		{
			return email.IndexOf ('\"') < 0 ? email : email.Split ('\"') [1];
		}

		public static string ParseEmailAddress(string email)
		{
			return email.IndexOf ('<') < 0 ? email : email.Split ('<') [1].Split('>') [0];
		}

		public override string ToString ()
		{
			return $"Email={Email}, Count={ReplyTimesMinutes.Count}, Reply Average={ReplyTimesAverage}";
		}

		public static string MinutesToString (double minutes)
		{
			var time = TimeSpan.FromMinutes(minutes);

			if (time.Days > 0)
				return $"{time:d\\dh\\h}";

			return time.Hours > 0 ? $"{time:h\\hmm\\m}" : $"{time:m\\m}";
		}
	}

	class Email
	{
		[PrimaryKey]
		public string Id {get;set;}
		public string Subject {get;set;}
		public string From {get;set;}
		public string InReplyTo { get; set; }
		public DateTimeOffset Date { get; set; }	
		string to;
		string[] toSeparated;

		public string To {
			get {
				return to;
			}
			set {
				to = value;
				toSeparated = value.Split (',');
			}
		}

		[Ignore]
		public string[] ToSeparated {
			get {
				return toSeparated;
			}
			set {
				toSeparated = value;
				to = string.Join (",", value);
			}
		}

	}

	public static class MainClass
	{
		private static IMailFolder GetMailbox (string email, string password)
		{
			var client = new ImapClient ();

			client.Connect ("imap.gmail.com", 993, true);
			client.AuthenticationMechanisms.Remove ("XOAUTH");
			client.Authenticate (email, password);

			var mailbox =  client.GetFolder ("[Gmail]/All Mail");
			mailbox.Open (FolderAccess.ReadOnly);
			return mailbox;
		}

		private static void AppendToEmailsDict (Dictionary<string, List<int>> dict, string key, int value) 
		{
			if (dict.ContainsKey (key)) {
				dict [key].Add (value);
			} else {
				dict [key] = new List<int> ();
				dict [key].Add (value);
			}
		}

		//
		// Ideas:
		// - Leaderboard of people you email with the most, with median response times
		// - Graph of # of emails sent/received by time of day
		// - Per-person stats: # of emails, avg, min, max thread lengths.
		public static Dictionary<string,EmailData> CalculateStatistics (string myEmailAddress, int daysAgo)
		{
			var minDate = (DateTimeOffset) DateTime.Now.AddDays(-daysAgo);
			var emails = Database.Main.Table<Email> ().Where (x => x.Date > minDate).ToList ();

			var emailsById = new Dictionary<string, Email> ();
			foreach (var email in emails) {
				emailsById [email.Id] = email;
			}

			var emailData = new Dictionary<string,EmailData> ();

			foreach (var email in emails) {

				if (email.Date < DateTime.Now.AddDays (-daysAgo))
					continue;

				bool isMyEmail = false;
				if (email.From.IndexOf (myEmailAddress, StringComparison.CurrentCultureIgnoreCase) > -1) {
					isMyEmail = true;
				}

				Email repliedFrom = null;
				if (!emailsById.TryGetValue (email.InReplyTo ?? "", out repliedFrom))
					continue;

				var key = isMyEmail ? repliedFrom.From : email.From;

				EmailData data;
				if (!emailData.TryGetValue (key, out data)) {
					emailData [key] = data = new EmailData {
						Email = key,
					};
				}

				var replyTime = (int)(email.Date - repliedFrom.Date).TotalMinutes;

				if (isMyEmail)
					data.MyReplyTimes [email.Id] = replyTime;
				else {
					if (repliedFrom.From.IndexOf (myEmailAddress, StringComparison.CurrentCultureIgnoreCase) > -1) {
						data.ReplyTimesMinutes [email.Id] = replyTime;
					} else {
						data.OtherReplyTimesMinutes [email.Id] = replyTime;
					}
				}
			}

			var items = emailData.Values.OrderBy(x=> x.ReplyTimesAverage).ToList();

			items.ForEach(Console.WriteLine);

			return emailData;
		}

		public static async Task FetchNewEmails (string myEmailAddress, string password, int daysAgo)
		{
			var fetchStart = DateTime.Now.AddDays (-daysAgo);
			var fetchEnd = DateTime.Now;

			var syncState = Database.Main.Table<SyncState> ().FirstOrDefault (x => x.EmailAddress == myEmailAddress);

			//if (syncState?.DownloadEnd > DateTime.Now.AddMinutes (-60)) {
			//	Console.WriteLine ("Email fetch already performed in the last hour; skipping...");
				//return;
//			} 
			
			if (fetchStart > syncState?.DownloadStart && fetchStart < syncState?.DownloadEnd)
				fetchStart = syncState.DownloadEnd;

			var start = DateTime.Now;
			var inbox = GetMailbox (myEmailAddress, password);

			// Make sure we search at least one day to work around a strange behavior where
			// Google is returning the entire mailbox.
			if ((fetchEnd - fetchStart).TotalDays < 1)
				fetchStart = fetchStart.AddDays (-1);

			var query = SearchQuery.DeliveredAfter (fetchStart).And (SearchQuery.DeliveredBefore (fetchEnd));
			var newUids = inbox.Search (query);

			Console.WriteLine ("Search got {0} total emails in {1} seconds", newUids.Count, (DateTime.Now - start).TotalSeconds);

			start = DateTime.Now;
			var emails = inbox.Fetch (newUids, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId);
			Console.WriteLine("Fetched {0} email headers in {1} seconds", emails.Count, (DateTime.Now - start).TotalSeconds);

			var newEmails = emails.Select (mail => new Email { 
				Id = mail.Envelope.MessageId,
				Subject = mail.Envelope.Subject,
				From = mail.Envelope.From.ToString (),
				InReplyTo = mail.Envelope.InReplyTo,
				Date = (DateTimeOffset) mail.Envelope.Date,
				To = String.Join (",", mail.Envelope.To)
			});
				
			Database.Main.InsertOrReplaceAll (newEmails);

			var newSyncState = new SyncState ();
			newSyncState.EmailAddress = myEmailAddress;
			newSyncState.DownloadStart = fetchStart;
			if (syncState != null && syncState.DownloadStart < fetchStart)
				newSyncState.DownloadStart = syncState.DownloadStart;
			newSyncState.DownloadEnd = fetchEnd;
			Database.Main.InsertOrReplace(newSyncState);
		}

	}
}
