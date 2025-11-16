using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace PhoneStoreAdmin.Converters
{
    public sealed class ExpansionStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var isExpanded = value as bool? ?? false;
            var mode = (parameter as string)?.ToLowerInvariant() ?? "header";

            if (mode == "header")
            {
                if (isExpanded)
                {
                    return new SolidColorBrush(Colors.White);
                }

                var gradient = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1)
                };
                gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 236, 244, 255), Offset = 0 });
                gradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 255, 255, 255), Offset = 1 });
                return gradient;
            }

            if (mode == "border")
            {
                return isExpanded
                    ? new SolidColorBrush(Color.FromArgb(255, 231, 235, 255))
                    : new SolidColorBrush(Color.FromArgb(255, 209, 223, 255));
            }

            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
