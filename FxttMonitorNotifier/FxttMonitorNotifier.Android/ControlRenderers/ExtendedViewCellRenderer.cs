using FxttMonitorNotifier.Droid.ControlExtensions;
using FxttMonitorNotifier.Droid.ControlRenderers;

using Xamarin.Forms;

[assembly: ExportRenderer(typeof(ExtendedViewCell), typeof(ExtendedViewCellRenderer))]
namespace FxttMonitorNotifier.Droid.ControlRenderers
{
    using Android.Content;
    using Android.Graphics.Drawables;
    using Android.Views;

    using System.ComponentModel;

    using Xamarin.Forms.Platform.Android;

    public class ExtendedViewCellRenderer : ViewCellRenderer
    {
        private const string IsSelectedPropertyName = "IsSelected";

        private bool _isSelected;

        private View _cellCore;
        private Drawable _unselectedBackground;

        protected override View GetCellCore(Cell item, View convertView, ViewGroup parent, Context context)
        {
            _cellCore = base.GetCellCore(item, convertView, parent, context);

            _isSelected = false;
            _unselectedBackground = _cellCore.Background;

            return _cellCore;
        }

        protected override void OnCellPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            base.OnCellPropertyChanged(sender, args);

            if (args.PropertyName.Equals(IsSelectedPropertyName, System.StringComparison.Ordinal))
            {
                _isSelected ^= true;

                if (_isSelected)
                {
                    var extendedViewCell = sender as ExtendedViewCell;

                    var parsedColor = Color.FromHex(extendedViewCell.SelectedBackgroundColor);

                    _cellCore.SetBackgroundColor(parsedColor.ToAndroid());
                }
                else
                {
                    _cellCore.SetBackgroundColor(Color.Black.ToAndroid());
                }
            }
        }
    }
}