using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MailStats
{
	public class EmailScoreEntry
	{
		public string Email {get;set;}
		public string Name => ParseName (Email);
		public string EmailAddress => ParseEmailAddress (Email);
		public int EmailCount {get; set;}
		public double MeanReplyTime { get; set; }
		public string MeanReplyTimeString => MinutesToString (MeanReplyTime);

		public static string MinutesToString (double minutes)
		{
			var time = TimeSpan.FromMinutes(minutes);

			if (time.Days > 0)
				return $"{time:d\\dh\\h}";

			return time.Hours > 0 ? $"{time:h\\hmm\\m}" : $"{time:m\\m}";
		}

		public static string ParseName(string email)
		{
			return email.IndexOf ('\"') < 0 ? email : email.Split ('\"') [1];
		}

		public static string ParseEmailAddress(string email)
		{
			return email.IndexOf ('<') < 0 ? email : email.Split ('<') [1].Split('>') [0];
		}

		public EmailScoreEntry(EmailData data, bool from_me)
		{
			this.Email = data.Email;

			if (from_me) {
				this.EmailCount = data.MyReplyTimesCount;
				this.MeanReplyTime = data.MyReplyTimesAverage;
			} else {
				this.EmailCount = data.ReplyTimesCount;
				this.MeanReplyTime = data.ReplyTimesAverage;
			}
		}
	}

	public class EmailData
	{
		public string Email {get;set;}

		// Their responses to emails from me
		public Dictionary<string,int> ReplyTimesMinutes { get; set; } = new Dictionary<string,int>();
		public double ReplyTimesAverage => ReplyTimesMinutes.Count == 0 ? 0 : ReplyTimesMinutes.Values.Average();
		public int ReplyTimesMin => ReplyTimesMinutes.Count == 0 ? 0 : ReplyTimesMinutes.Values.Min();
		public int ReplyTimesMax => ReplyTimesMinutes.Count == 0 ? 0 : ReplyTimesMinutes.Values.Max();
		public int ReplyTimesCount => ReplyTimesMinutes.Count;

		// My responses to emails from them
		public Dictionary<string,int> MyReplyTimes { get; set; } = new Dictionary<string,int>();
		public double MyReplyTimesAverage => MyReplyTimes.Count == 0 ? 0 : MyReplyTimes.Values.Average();
		public int MyReplyTimesMin => MyReplyTimes.Count == 0 ? 0 : MyReplyTimes.Values.Min();
		public int MyReplyTimesMax => MyReplyTimes.Count == 0 ? 0 : MyReplyTimes.Values.Max();
		public int MyReplyTimesCount => MyReplyTimes.Count;

		// Their responses to emails from other people
		public Dictionary<string,int> OtherReplyTimesMinutes { get; set; } = new Dictionary<string,int>();
		public double OtherReplyTimesAverage => OtherReplyTimesMinutes.Count == 0 ? 0 : OtherReplyTimesMinutes.Values.Average();
		public int OtherReplyTimesMin => OtherReplyTimesMinutes.Count == 0 ? 0 : OtherReplyTimesMinutes.Values.Min();
		public int OtherReplyTimesMax => OtherReplyTimesMinutes.Count == 0 ? 0 : OtherReplyTimesMinutes.Values.Max();
		public int OtherReplyTimesCount => OtherReplyTimesMinutes.Count;

		public override string ToString ()
		{
			return $"Email={Email}, Count={ReplyTimesMinutes.Count}, Reply Average={ReplyTimesAverage}";
		}
	}

	public class CalcStats 
	{
		// FIXME only count replies, not forwards
		public static Dictionary<string,EmailData> CalculateStatistics (int daysAgo)
		{
			var myEmailAddress = App.GoogleUser.Email;
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

			return emailData;
		}
	}
}

