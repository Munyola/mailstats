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

	public class ScoreboardHeader : Grid
	{
		public Button email, count, mean;

		public ScoreboardHeader ()
		{
			var fontSize = 16;

			email = new Button {
				Text = "Person",
				FontAttributes = FontAttributes.Bold,
				FontSize = fontSize
			};

			count = new Button {
				Text = "Count",
				FontAttributes = FontAttributes.Bold,
				FontSize = fontSize
			};

			mean = new Button {
				Text = "Reply Time",
				FontAttributes = FontAttributes.Bold,
				FontSize = fontSize
			};

			count.Clicked += (object sender, EventArgs e) => {
				var model = (MainPageViewModel) BindingContext;
				model.CurrentSort = "EmailCount";
				model.CurrentSortAscending = ! model.CurrentSortAscending;
				model.FilterSort ();
			};

			email.Clicked += (object sender, EventArgs e) => {
				var model = (MainPageViewModel) BindingContext;
				model.CurrentSort = "Name";
				model.CurrentSortAscending = ! model.CurrentSortAscending;
				model.FilterSort ();
			};

			mean.Clicked += (object sender, EventArgs e) => {
				var model = (MainPageViewModel) BindingContext;
				model.CurrentSort = "MeanReplyTime";
				model.CurrentSortAscending = ! model.CurrentSortAscending;
				model.FilterSort ();
			};

			ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (4, GridUnitType.Star) });
			ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (1, GridUnitType.Star) });
			ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (2, GridUnitType.Star) });

			Padding = 10;

			Children.Add (email, 0, 0);
			Children.Add (count, 1, 0);
			Children.Add (mean, 2, 0);
		}
	}
	
}
