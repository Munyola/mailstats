using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Linq;

namespace SegmentedControl
{
	public class SegmentedControl : View, IViewContainer<SegmentedControlOption>
	{
		public IList<SegmentedControlOption> Children { get; set; }

		public SegmentedControl ()
		{
			Children = new List<SegmentedControlOption> ();
		}

		public event EventHandler ValueChanged;



		public string SelectedValue {
			get{ 
				if (Children.Count >= SelectedIndex)
					return null;
				return SelectedValue = Children [selectedIndex].Text;; }
			set {
				var match = Children.FirstOrDefault (x=> x.Text == value);
				if (match == null)
					return;
				var index = Children.IndexOf (match);
				SelectedIndex = index;
			}
		}

		private int selectedIndex = -1;
		public int SelectedIndex {
			get{ return selectedIndex; }
			set {
				if (selectedIndex == value)
					return;
				selectedIndex = value;
				SelectedValue = Children [value].Text;
				if (ValueChanged != null)
					ValueChanged (this, EventArgs.Empty);
			}
		}
	}

	public class SegmentedControlOption:View
	{
		public static readonly BindableProperty TextProperty = BindableProperty.Create<SegmentedControlOption, string> (p => p.Text, "");

		public string Text {
			get{ return (string)GetValue (TextProperty); }
			set{ SetValue (TextProperty, value); }
		}

		public SegmentedControlOption ()
		{
		}
	}
}