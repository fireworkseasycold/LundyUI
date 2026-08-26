using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LundyUI.WPF.Converters
{
	/// <summary>
	/// 把 double 转换为其原值，仅当值为 NaN 时返回 0。
	/// 用于 GridSplitter 扇柄 RotateTransform.Angle 的绑定：SplitterFanAngle 默认值是 double.NaN（表示"自动"），
	/// 而 Transform 角度若是 NaN 会导致 layout 算出 NaN 的 desired size 并抛 Measure(NaN)，故需归一化。
	/// </summary>
	public sealed class NaNToZeroConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double d && double.IsNaN(d))
			{
				return 0.0;
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> DependencyProperty.UnsetValue;
	}
}