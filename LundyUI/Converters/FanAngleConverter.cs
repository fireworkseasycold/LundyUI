using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace LundyUI.WPF.Converters
{
	/// <summary>
	/// 计算 GridSplitter 手柄的旋转角度。
	/// 注意：公开属性 ResizeDirection 在自动布局下读到的常是 Auto，不可靠，
	/// 因此当角度未显式设置时，按实际几何(宽/高)判断方向。
	/// values[0] = (lui:Controls.SplitterFanAngle)     double，默认 NaN 表示未设置
	/// values[1] = ResizeDirection
	/// values[2] = ActualWidth
	/// values[3] = ActualHeight
	/// 竖条(窄而高)→ -90°(尖朝左+右线)；横条(宽而矮)→ 0°(尖朝上+下线)。
	/// 用户显式设置角度时优先使用用户值。
	/// </summary>
	public sealed class FanAngleConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{

			double fan = double.NaN;
			if (values != null && values.Length > 0 && values[0] is double d)
			{
				fan = d;
			}

			if (!double.IsNaN(fan))
			{
				return fan;
			}

			// 未显式设置 → 按方向判断
			if (values != null && values.Length > 1 && values[1] is GridResizeDirection dir)
			{
				if (dir == GridResizeDirection.Columns) return -90.0;
				if (dir == GridResizeDirection.Rows) return 0.0;
			}

			// Auto（实际多数情况）：按几何。竖条(窄高)=Columns=-90；横条(宽矮)=Rows=0
			double w = values != null && values.Length > 2 && values[2] is double ww ? ww : 0.0;
			double h = values != null && values.Length > 3 && values[3] is double hh ? hh : 0.0;
			if (w == 0.0 && h == 0.0) return 0.0;      // 首帧无法判断，暂横
			return h >= w ? -90.0 : 0.0;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}