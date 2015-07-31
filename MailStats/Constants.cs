using System;

namespace MailStats
{
	public class Constants
	{
		// Google OAuth
		public static string ClientId = "900358175438-rrk0rqp76dd5l24jsevmunjm1277an50.apps.googleusercontent.com";
		public static string ClientSecret = "W6CsgvukJ3YxWrhSs98B7Q22";
		public static string Scope = "https://www.googleapis.com/auth/gmail.readonly";
		public static string AuthorizeUrl = "https://accounts.google.com/o/oauth2/auth";
		public static string RedirectUrl = "http://localhost";
		public static string AccessTokenUrl = "https://accounts.google.com/o/oauth2/token";
	}
}

