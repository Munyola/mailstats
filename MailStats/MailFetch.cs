using System;
using System.Collections.Generic;
using System.Linq;

using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;

using System.Threading.Tasks;
using System.Net;

namespace MailStats
{

	public static class MailFetch
	{
		private static IMailFolder GetMailbox ()
		{
			var client = new ImapClient ();

			var credentials = new NetworkCredential (App.GoogleUser.Email, App.GoogleUser.AccessToken);

			client.Connect ("imap.gmail.com", 993, true);
			try {
				client.Authenticate (credentials);
			}
			catch (Exception e)
			{
				// FIXME: Handle this error correctly
				Xamarin.Insights.Report (e);
				Console.WriteLine("Got exception: #{0}", e);
				throw;
			}

			var mailbox =  client.GetFolder ("[Gmail]/All Mail");
			mailbox.Open (FolderAccess.ReadOnly);
			return mailbox;
		}

		public static int NumEmails()
		{
			return Database.Main.ExecuteScalar<int> ("SELECT COUNT(*) from Email;");
		}

		public delegate void FetchEmailProgressCallback(int percent, int emailsFetched, int totalEmails);

		public class EmailId {
			public string UniqueId {get; set;} 
		}

		public static async Task FetchNewEmails (int daysAgo, FetchEmailProgressCallback progressCallback = null)
		{
			var myEmailAddress = App.GoogleUser.Email;

			// Determine the range of dates to search in email
			var fetchStart = DateTime.Now.AddDays (-daysAgo);
			var fetchEnd = DateTime.Now;

			// Make sure we search at least one day to work around a strange behavior where
			// Google is returning the entire mailbox.
			if ((fetchEnd - fetchStart).TotalDays < 1)
				fetchStart = fetchStart.AddDays (-1);

			// Figure out what date range of emails we already have in the database
			var syncState = Database.Main.Table<SyncState> ().FirstOrDefault (x => x.EmailAddress == myEmailAddress);
			if (syncState?.DownloadEnd > DateTime.Now.AddMinutes (-60) && syncState.DownloadStart < fetchStart) {
				Console.WriteLine ("Email fetch already performed in the last hour; skipping...");
				return;
			} 
			if (fetchStart > syncState?.DownloadStart && fetchStart < syncState?.DownloadEnd)
				fetchStart = syncState.DownloadEnd;

			// Get a list of all email IDs in our database
			var dbEmailIds = Database.Main.Query<EmailId> ("select UniqueId from Email;");
			var idHash = new Dictionary<string,bool> ();
			foreach (var emailId in dbEmailIds)
				idHash.Add (emailId.UniqueId.ToString (), true);
			
			// Log in to IMAP and get a handle to the All Mail folder
			var start = DateTime.Now;
			var inbox = GetMailbox ();

			// Fetch a list of all emails to search
			var query = SearchQuery.DeliveredAfter (fetchStart).And (SearchQuery.DeliveredBefore (fetchEnd));
			var uids = inbox.Search (query);

			// Remove any IDs that are already present in the database
			var newUids = new List<UniqueId> ();
			foreach (var id in uids) {
				if (idHash.ContainsKey (id.ToString ()) == false)
					newUids.Add (id);
			}
			// FIXME: for some reason this LINQ version doesn't work :-( 
			// var newUids = uids.Where(id => idHash.ContainsKey(id.ToString ()) == false).ToList();

			Console.WriteLine ("Search got {0} total emails, {1} new emails, in {2} seconds",
				uids.Count, newUids.Count, (DateTime.Now - start).TotalSeconds);

			// Split the set of emails to grab into chunks of 100 so we can report progress
			var sublists = newUids
				.Select((x, i) => new { Index = i, Value = x })
				.GroupBy(x => x.Index / 100)
				.Select(x => x.Select(v => v.Value).ToList())
				.ToArray();

			var emailsFetched = 0;
			foreach (var sublist in sublists) {

				IList<IMessageSummary> emails = null;
				try {
					emails = inbox.Fetch (sublist, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId);
					emailsFetched += sublist.Count;
					if (progressCallback != null)
						progressCallback (emailsFetched * 100 / newUids.Count, emailsFetched, newUids.Count);
				} catch (Exception e) {
					Xamarin.Insights.Report (e);
					Console.WriteLine (e);
				}
				foreach (var mail in emails) {
					if (mail == null)
						Console.WriteLine ("Null mail!");
					if (mail?.Envelope == null)
						Console.WriteLine ("Null Envelope!");
				}

				try {
					var newEmails = emails.Select (mail => new Email { 
						Id = mail.Envelope.MessageId,
						UniqueId = mail.UniqueId.ToString (),
						Subject = mail.Envelope.Subject,
						From = mail.Envelope.From.ToString (),
						InReplyTo = mail.Envelope.InReplyTo,
						Date = (DateTimeOffset)mail.Envelope.Date,
						To = String.Join (",", mail.Envelope.To)
					});
						
					Database.Main.InsertOrReplaceAll (newEmails);
				} catch (Exception e) {
					Xamarin.Insights.Report (e);
				}
			
			}

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
