using System;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;

namespace MailStats
{
	public class GmailFetch
	{
		private static IMailFolder GetMailbox (string email, string password)
		{
			var client = new ImapClient ();

			client.Connect ("imap.gmail.com", 993, true);
			//client.AuthenticationMechanisms.Remove ("XOAUTH");

			client.Authenticate (email, password);

			var mailbox =  client.GetFolder ("[Gmail]/All Mail");
			mailbox.Open (FolderAccess.ReadOnly);
			return mailbox;
		}

		public GmailFetch ()
		{
		}
	}
}

