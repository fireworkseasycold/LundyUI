using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LundyUI.Controls.Converters
{
    /// <summary>字符串非空白 -> Visible，否则 -> Collapsed（UI 框架级，通用）。</summary>
    public sealed class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
