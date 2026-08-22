using System.Windows.Controls;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// LundyUI 加载圈（替代 HandyControl LoadingCircle）。
/// 用法：<c>&lt;controls:LoadingCircleControl Width="56" Height="56" Foreground="..."/&gt;</c>。
/// </summary>
public partial class LoadingCircleControl : UserControl
{
	public LoadingCircleControl()
	{
		InitializeComponent();
	}
}