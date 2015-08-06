using System;
using Xamarin.Forms;

namespace MailStats
{
    public class ScoreboardEntryCell : ViewCell
    {

        protected Label name, email, count, mean, min, max;

        public ScoreboardEntryCell ()
        {
            var fontSize = 12;
            IsEnabled = false;

            name = new Label ();
            name.FontSize = fontSize;
            name.SetBinding (Label.TextProperty, "Name");

            // FIXME: Can't figure out how to make this light grey.
            email = new Label ();
            email.FontSize = fontSize - 2;
            email.SetBinding (Label.TextProperty, "EmailAddress");

            count = new Label ();
            count.FontSize = fontSize;
            count.XAlign = TextAlignment.End;

            count.SetBinding (Label.TextProperty, "EmailCountString");

            mean = new Label ();
            mean.FontSize = fontSize;
            mean.XAlign = TextAlignment.End;
            mean.SetBinding (Label.TextProperty, "MeanReplyTimeString");

            var grid = new Grid {
                Padding = new Thickness (5),
                ColumnDefinitions = {
                    new ColumnDefinition { Width = new GridLength (4, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength (1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength (2, GridUnitType.Star) }
                }
            };

            var stack = new StackLayout {
                Spacing = 0, 
                Children = {
                    name,
                    email
                }
            };

            grid.Children.Add (stack, 0, 0);
            grid.Children.Add (count, 1, 0);
            grid.Children.Add (mean, 2, 0);

            View = grid;
        }
    }

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

            ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (1, GridUnitType.Star) });
            ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (1, GridUnitType.Auto) });
            ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (1, GridUnitType.Auto) });

            Padding = 10;

            Children.Add (email, 0, 0);
            Children.Add (count, 1, 0);
            Children.Add (mean, 2, 0);
        }
    }

}

