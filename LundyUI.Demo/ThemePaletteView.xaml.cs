using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LundyUI.Demo;

/// <summary>
/// 主题样板页：格子平铺展示全部主题键在基础控件上的实际效果，
/// 用于统一确认 UI 配色规范（文本/背景/下拉框等一律使用本页标注的 key 绑定）。
/// 点击任意展示格弹出放大预览，解决格子过小看不清细节的问题。
/// </summary>
public partial class ThemePaletteView : UserControl
{
	public ThemePaletteView()
	{
		InitializeComponent();
	}

	/// <summary>
	/// 点击展示格弹出放大预览：克隆格子内容为真实尺寸实例（XamlWriter 保留 DynamicResource
	/// 表达式，克隆体与 App 资源环境联通 → 主题切换实时跟随；DatePicker/Calendar 等展开的
	/// Popup 弹层为真实大小，可直接交互验证，不再受格子尺寸限制）。
	/// </summary>
	private void OnCellClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is not Border cell || cell.Child is not StackPanel panel) return;

		// 标题取格子内第一个 TextBlock 文字（key 名）
		string? title = null;
		foreach (var child in panel.Children)
		{
			if (child is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text))
			{
				title = tb.Text.Trim();
				break;
			}
		}
		OverlayTitle.Text = (string.IsNullOrEmpty(title) ? "样式预览" : title) + "（点遮罩或右上角 ✕ 关闭）";

		OverlayHost.Content = CloneForPreview(panel, cell);
		Overlay.Visibility = Visibility.Visible;
	}

	/// <summary>
	/// 克隆格子内容为真实尺寸预览实例：移除 key 名标题行，清除 Control 显式尺寸（恢复自然尺寸）。
	/// 克隆失败（罕见，如命名冲突）时回退原格子等比放大，保证预览不中断。
	/// </summary>
	private static FrameworkElement CloneForPreview(StackPanel panel, Border cell)
	{
		try
		{
			var clone = (StackPanel)XamlReader.Parse(XamlWriter.Save(panel));
			if (clone.Children.Count > 0 && clone.Children[0] is TextBlock)
				clone.Children.RemoveAt(0);
			CopyDataGridColumns(panel, clone);
			ResetControlSizes(clone);
			return clone;
		}
		catch
		{
			// 回退：原格子等比放大（VisualBrush 矢量缩放不模糊）
			return new Rectangle
			{
				Width = Math.Max(cell.ActualWidth, 1),
				Height = Math.Max(cell.ActualHeight, 1),
				Fill = new VisualBrush(cell) { Stretch = Stretch.Fill }
			};
		}
	}

	/// <summary>
	/// XamlWriter.Save 不序列化 DataGrid.Columns（列集合），克隆后按原格位置手动复制列，
	/// 保证放大预览能看到列头/列宽（ListView 的 GridView 列 Save 会保留，无需处理）。
	/// </summary>
	private static void CopyDataGridColumns(StackPanel source, StackPanel target)
	{
		int tIdx = 0;
		for (int sIdx = 0; sIdx < source.Children.Count && tIdx < target.Children.Count; sIdx++)
		{
			// 原格子第一个 child 是 key 名 TextBlock（克隆时已移除），跳过对齐
			if (sIdx == 0 && source.Children[sIdx] is TextBlock) continue;
			if (source.Children[sIdx] is DataGrid srcDg && target.Children[tIdx] is DataGrid dstDg)
			{
				foreach (DataGridColumn col in srcDg.Columns)
				{
					try { dstDg.Columns.Add((DataGridColumn)XamlReader.Parse(XamlWriter.Save(col))); }
					catch { /* 单列复制失败忽略：行样式仍可见 */ }
				}
			}
			tIdx++;
		}
	}

	/// <summary>递归清除 Control 的显式 Width/Height（恢复自然尺寸）；Border/TextBlock 等装饰元素保留原尺寸</summary>
	private static void ResetControlSizes(DependencyObject root)
	{
		// 进度条模板没有默认自然高度，清除 Height 后会塌陷导致放大预览看不到，保持原尺寸
		if (root is ProgressBar) return;

		if (root is Control control)
		{
			if (!double.IsNaN(control.Width)) control.Width = double.NaN;
			if (!double.IsNaN(control.Height)) control.Height = double.NaN;
		}
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
			ResetControlSizes(VisualTreeHelper.GetChild(root, i));
	}

	private void OnCloseOverlay(object sender, MouseButtonEventArgs e) => Overlay.Visibility = Visibility.Collapsed;

	/// <summary>
	/// 点击遮罩空白处关闭（点卡片内部不关闭）。
	/// </summary>
	private void OnOverlayClick(object sender, MouseButtonEventArgs e)
	{
		if (e.OriginalSource == Overlay) Overlay.Visibility = Visibility.Collapsed;
	}
}
