using System;
using System.Globalization;
using System.Windows.Data;

namespace LundyUI.Controls.Converters;

/// <summary>
/// 布尔值 → 展开/折叠符号（▲/▼）。
/// 用于菜单分类头的展开状态指示。
/// </summary>
public sealed class BoolToExpandSymbolConverter : IValueConverter
{
    public static readonly BoolToExpandSymbolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            return "▲";
        }
        return "▼";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
