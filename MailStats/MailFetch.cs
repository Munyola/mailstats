using System;
using System.Collections.Generic;
using System.Linq;

using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;

using SQLite;
using System.IO;

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

	public class ScoreboardEntry 
	{
		public string Email {get; set;}
		public int MyReplyCount {get; set;}
		public int TheirReplyCount {get; set;}

		public int MyMeanReply {get; set;}
		public int MyMedianReply {get; set;}
		public int MyMinReply {get; set;}
		public int MyMaxReply {get; set;}

		public int TheirMeanReply {get; set;}
		public int TheirMedianReply {get; set;}
		public int TheirMinReply {get; set;}
		public int TheirMaxReply {get; set;}
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
		public static Dictionary<string, ScoreboardEntry> CalculateStatistics (string myEmailAddress, int daysAgo)
		{
			var emails = Database.Main.Query<Email> ("SELECT * from Email;");

			var emailsById = new Dictionary<string, Email> ();
			foreach (var email in emails) {
				emailsById [email.Id] = email;
			}

			var replyTimesByEmail = new Dictionary<string, List<int>> ();
			var myReplyTimes = new List<int> ();
			var myReplyTimesByEmail = new Dictionary<string, List<int>> ();

			int emailsWithReplies = 0;

			foreach (var email in emails) {

				if (email.Date < DateTime.Now.AddDays (-daysAgo))
					continue;

				if (email.InReplyTo != null && emailsById.ContainsKey(email.InReplyTo)) {
					emailsWithReplies ++;
					var orig = emailsById [email.InReplyTo];
					int replyDelay = (int)(email.Date - orig.Date).TotalMinutes;

					if (email.From.Contains (myEmailAddress) && !orig.From.Contains (myEmailAddress)) {
						myReplyTimes.Add (replyDelay);
						AppendToEmailsDict (myReplyTimesByEmail, orig.From, replyDelay);
					} else if (email.To.Contains (myEmailAddress) && orig.From.Contains (myEmailAddress)) {
						AppendToEmailsDict (replyTimesByEmail, email.From, replyDelay);
					}
				}
			}

			var scoreboardEntries = new Dictionary<string,ScoreboardEntry> ();

			Console.WriteLine ("Emails w/ replies: {0}", emailsWithReplies);

			Console.WriteLine ("My average reply time, based on {0} replies by me", myReplyTimes.Count);
			Console.WriteLine ("\t{0} minutes - mean reply time (me to them)", myReplyTimes.Average ().ToString ("F"));

			Console.WriteLine ("My average reply time, me to them");
			var items = from item in myReplyTimesByEmail
					where item.Value.Count > 2
				orderby item.Value.Average() descending
				select item;

			foreach (var item in items) {
//				var score = new ScoreboardEntry ();
//				score.Email = item.Key;
//				score.MyReplyCount = item.Value.Count;
//				score.MyMeanReply = item.Value.Average ();
//				score.MyMinReply = item.Value.Min ();
//				score.MyMaxReply = item.Value.Max ();
//
//				scoreboardEntries [score.Email] = score;

				Console.WriteLine ("\t{0} - {1} emails, {2}m (mean), {3}m (min), {4}m (max)", 
					item.Key, item.Value.Count, item.Value.Average ().ToString ("F"), item.Value.Min (), item.Value.Max ());
			}

			Console.WriteLine ("Their average reply time, them to me");
			items = from item in replyTimesByEmail
					where item.Value.Count > 2
				orderby item.Value.Average() descending
				select item;

			foreach (var item in items) {
				ScoreboardEntry entry = null;
				entry = new ScoreboardEntry ();
				entry.Email = item.Key;
				entry.TheirReplyCount = item.Value.Count;
				entry.TheirMeanReply = (int) item.Value.Average ();
				entry.TheirMinReply = item.Value.Min ();
				entry.TheirMaxReply = item.Value.Max ();
				scoreboardEntries [item.Key] = entry;


				Console.WriteLine ("\t{0} - {1} emails, {2}m (mean), {3}m (min), {4}m (max)", 
					item.Key, item.Value.Count, item.Value.Average ().ToString ("F"), item.Value.Min (), item.Value.Max ());
			}

			return scoreboardEntries;
		}

		public static void FetchNewEmails (string myEmailAddress, string password, int daysAgo)
		{
			var fetchStart = DateTime.Now.AddDays (-daysAgo);
			var fetchEnd = DateTime.Now;

			var syncState = Database.Main.Table<SyncState> ().FirstOrDefault (x => x.EmailAddress == myEmailAddress);

			if (syncState?.DownloadEnd > DateTime.Now.AddMinutes (-60)) {
				Console.WriteLine ("Email fetch already performed in the last hour; skipping...");
				return;
			} 
			
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
				Date = (DateTimeOffset)mail.Envelope.Date,
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
