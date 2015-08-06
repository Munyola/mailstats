using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Widget;
using System.Collections.Generic;

[assembly:ExportRenderer(typeof(SegmentedControl.SegmentedControl), typeof(SegmentedControl.Droid.SegmentedControlRenderer))]
namespace SegmentedControl.Droid
{
	public class SegmentedControlRenderer : ViewRenderer<SegmentedControl, Spinner>
	{
		public SegmentedControlRenderer ()
		{
		}

		protected override void OnElementChanged (ElementChangedEventArgs<SegmentedControl> e)
		{
			base.OnElementChanged (e);

			if (e.NewElement == null)
				return;

            var spinner = new Spinner(Forms.Context);
            var layoutParams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, 
                                  LinearLayout.LayoutParams.WrapContent);

            spinner.LayoutParameters = layoutParams;

            var list = new List<string>();
            for (var i = 0; i < e.NewElement?.Children.Count; i++)
            {
                list.Add(e.NewElement.Children[i].Text);
            }


            e.NewElement.ValueChanged += (sender, ev) => {
                var seg = sender as SegmentedControl;
                spinner.SetSelection(seg.SelectedIndex);
            };

            spinner.ItemSelected += (sender, ee) => 
                {
                    e.NewElement.SelectedIndex = spinner.SelectedItemPosition;
                };
            
            var adapter = new ArrayAdapter<string>(Forms.Context, Android.Resource.Layout.SimpleSpinnerItem, list.ToArray());

            adapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;

            spinner.SetSelection(e.NewElement.SelectedIndex);

            SetNativeControl (spinner);
		}

      
	}
}