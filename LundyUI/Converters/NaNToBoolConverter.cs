using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LundyUI.WPF.Converters;

/// <summary>
/// double.IsNaN → True；其他 → False。
/// GridSplitter 样式中：未显式设置 FanAngle 时由触发器补默认角度。
/// </summary>
public sealed class NaNToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return double.IsNaN(d);
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}