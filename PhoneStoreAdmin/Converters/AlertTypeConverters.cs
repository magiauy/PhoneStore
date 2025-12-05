using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace PhoneStoreAdmin.Converters
{
    /// <summary>
    /// Converter để chuyển đổi IsRecoveryAlert boolean thành màu sắc phù hợp
    /// Recovery (true) = Success (xanh lá), Price Drop (false) = Error (đỏ)
    /// </summary>
    public class BoolToAlertColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isRecovery = value is bool b && b;
            
            // Lấy màu từ Application Resources
            if (Application.Current.Resources.TryGetValue(
                isRecovery ? "BrushSuccess" : "BrushError", 
                out var brush))
            {
                return brush;
            }
            
            // Fallback colors
            return new SolidColorBrush(isRecovery 
                ? Colors.Green 
                : Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để chuyển đổi IsRecoveryAlert boolean thành background color (với opacity thấp)
    /// </summary>
    public class BoolToAlertBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isRecovery = value is bool b && b;
            
            // Tạo màu với opacity thấp để làm background
            var baseColor = isRecovery 
                ? Color.FromArgb(26, 0, 200, 83)   // Success với ~10% opacity
                : Color.FromArgb(26, 239, 68, 68); // Error với ~10% opacity
            
            return new SolidColorBrush(baseColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
