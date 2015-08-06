using System;
using SQLite;
using System.IO;
using System.Collections.Generic;

namespace MailStats
{
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
		public string Id {get; set;} // The MessageId from the mail header, used by In-Reply-To
		public string UniqueId {get; set; } // The IMAP ID
		public string Subject {get; set;}
		public string From {get; set;}
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

	class Locations
	{
		#if __IOS__
        public static string BaseDir = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Personal)).ToString();
        public static readonly string LibDir = Path.Combine(BaseDir, "Library/");
        #else 
        public static readonly string LibDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        #endif
	}
}

