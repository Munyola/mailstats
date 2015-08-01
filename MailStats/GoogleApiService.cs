using System;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading;

namespace MailStats
{
	public static partial class Extensions
	{
		public static string TrimStart(this string s, string toTrim)
		{
			if(s.StartsWith(toTrim, true, Thread.CurrentThread.CurrentCulture))
				return s.Substring(toTrim.Length);

			return s;
		}

		public static string Fmt(this string s, params object[] args)
		{
			return string.Format(s, args);
		}

	}

	public class UserProfile
	{
		[JsonProperty("id")]
		public string Id
		{
			get;
			set;
		}

		[JsonProperty("email")]
		public string Email
		{
			get;
			set;
		}

		[JsonProperty("verified_email")]
		public bool VerifiedEmail
		{
			get;
			set;
		}

		[JsonProperty("name")]
		public string Name
		{
			get;
			set;
		}

		[JsonProperty("given_name")]
		public string GivenName
		{
			get;
			set;
		}

		[JsonProperty("family_name")]
		public string FamilyName
		{
			get;
			set;
		}

		[JsonProperty("link")]
		public string Link
		{
			get;
			set;
		}

		[JsonProperty("picture")]
		public string Picture
		{
			get;
			set;
		}

		[JsonProperty("gender")]
		public string Gender
		{
			get;
			set;
		}

		[JsonProperty("locale")]
		public string Locale
		{
			get;
			set;
		}

		[JsonProperty("hd")]
		public string Hd
		{
			get;
			set;
		}
	}


	public class GoogleApiService
	{
		#region Properties

		static GoogleApiService _instance;

		public static GoogleApiService Instance
		{
			get
			{
				return _instance ?? (_instance = new GoogleApiService());
			}
		}

		#endregion

		#region Authentication

		public Task<bool> ValidateToken(string token)
		{
			return new Task<bool>(() =>
				{
					var client = new HttpClient();
					string url = "https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=" + token.TrimStart("Bearer ");
					var json = client.GetStringAsync(url).Result;
					var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
					return result.ContainsKey("user_id");
				});
		}


		public UserProfile GetUserProfile(string access_token)
		{
			var client = new HttpClient();
			string url = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token=" + access_token;
			var json = client.GetStringAsync(url).Result;
			var profile = JsonConvert.DeserializeObject<UserProfile>(json);
			return profile;
		}

		public Task<Tuple<string, string>> GetAuthAndRefreshToken(string code)
		{
			return new Task<Tuple<string, string>>(() =>
				{
					string url = "https://www.googleapis.com/oauth2/v3/token";

					using(var client = new HttpClient())
					{
						var dict = new Dictionary<string, string>();
						dict.Add("code", code);
						dict.Add("grant_type", "authorization_code");
						dict.Add("redirect_uri", "urn:ietf:wg:oauth:2.0:oob");
						dict.Add("client_id", Constants.ClientId);
						dict.Add("client_secret", Constants.ClientSecret);

						var content = new FormUrlEncodedContent(dict);
						client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
						var response = client.PostAsync(url, content).Result;
						var body = response.Content.ReadAsStringAsync().Result;
						dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);

						if(!dict.ContainsKey("token_type"))
						{
							return null;
						}

						var newAuthToken = "{0} {1}".Fmt(dict["token_type"], dict["access_token"]);
						var refreshToken = dict["refresh_token"];
						return new Tuple<string, string>(newAuthToken, refreshToken);
					}
				});
		}

		public string GetNewAuthToken(string refreshToken)
		{
			const string url = "https://www.googleapis.com/oauth2/v3/token";

			using(var client = new HttpClient())
			{
				var dict = new Dictionary<string, string>();
				dict.Add("grant_type", "refresh_token");
				dict.Add("refresh_token", refreshToken);
				dict.Add("client_id", Constants.ClientId);
				dict.Add("client_secret", Constants.ClientSecret);

				var content = new FormUrlEncodedContent(dict);

				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
				var response = client.PostAsync(url, content).Result;
				var body = response.Content.ReadAsStringAsync().Result;
				dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);

				if(!dict.ContainsKey("token_type"))
				{
					return null;
				}

				return dict ["access_token"];
			}
		}

		#endregion
	}
}
