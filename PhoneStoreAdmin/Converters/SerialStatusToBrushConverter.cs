using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PhoneStoreRepository.Models.Enums;
using System;
using Windows.UI;

namespace PhoneStoreAdmin.Converters
{
    public sealed class SerialStatusToBrushConverter : IValueConverter
    {
        private static readonly byte BackgroundAlpha = 0x26;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not SerialStatus status)
            {
                return CreateBackgroundBrush(GetDefaultBrush());
            }

            var baseBrush = GetBaseBrush(status);
            if (parameter is string parameterText && parameterText.Equals("Foreground", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(baseBrush.Color);
            }

            return CreateBackgroundBrush(baseBrush);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }

        private static SolidColorBrush CreateBackgroundBrush(SolidColorBrush source)
        {
            var color = source.Color;
            return new SolidColorBrush(Color.FromArgb(BackgroundAlpha, color.R, color.G, color.B));
        }

        private static SolidColorBrush GetBaseBrush(SerialStatus status)
        {
            return status switch
            {
                SerialStatus.IN_STOCK => GetBrushFromResources("BrushSuccess"),
                SerialStatus.RESERVED => GetBrushFromResources("BrushWarning"),
                SerialStatus.SOLD => GetBrushFromResources("BrushPrimary"),
                SerialStatus.RETURNED => GetBrushFromResources("BrushAccent"),
                SerialStatus.DEFECTIVE => GetBrushFromResources("BrushError"),
                SerialStatus.RMA => GetBrushFromResources("BrushError"),
                _ => GetDefaultBrush()
            };
        }

        private static SolidColorBrush GetBrushFromResources(string resourceKey)
        {
            if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true && resource is SolidColorBrush brush)
            {
                return brush;
            }

            return GetDefaultBrush();
        }

        private static SolidColorBrush GetDefaultBrush()
        {
            return new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0x53, 0xF7));
        }
    }
}
