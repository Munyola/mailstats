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
				Console.WriteLine("Got exception: #{0}", e);
			}

			var mailbox =  client.GetFolder ("[Gmail]/All Mail");
			mailbox.Open (FolderAccess.ReadOnly);
			return mailbox;
		}

		public static int NumEmails()
		{
			return Database.Main.ExecuteScalar<int> ("SELECT COUNT(*) from Email;");
		}

		public static async Task FetchNewEmails (int daysAgo)
		{
			var myEmailAddress = App.GoogleUser.Email;
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
			var inbox = GetMailbox ();

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
