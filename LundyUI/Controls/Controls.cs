using System.Windows;
using System.Windows.Media;

namespace LundyUI.Controls;

/// <summary>
/// LundyUI 面向"原生基础控件"的附加属性集合。
///
/// 设计定位：基础控件（Button/TextBox 等）一律保持原生、用隐式样式做默认换肤（80% 场景）；
/// 需要"变体风格"时优先用语义化命名样式（如在同文件 `{x:Type Button}` 隐式样式的 Style.Triggers
/// 里已内置的 lui:Controls.Accent 触发器）。附加属性则解决"不改类型、不换前缀"前提下
/// 对单个实例的按需微调，对应 MahApps.ButtonHelper / MaterialDesign..Assist /
/// HandyControl 附加属性的同类思路。
/// </summary>
public static class Controls
{
	/// <summary>
	/// 圆角。
	/// 默认 3，与现有基础模板内硬编码圆角一致，保证不改视觉的前提下允许按元素覆盖。
	/// 已接线到 ButtonsAndToggles.xaml 中 Button/ToggleButton 族模板的边框 CornerRadius。
	/// </summary>
	public static readonly DependencyProperty CornerRadiusProperty =
		DependencyProperty.RegisterAttached(
			"CornerRadius",
			typeof(CornerRadius),
			typeof(Controls),
			new FrameworkPropertyMetadata(
				new CornerRadius(3),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static void SetCornerRadius(DependencyObject element, CornerRadius value) => element.SetValue(CornerRadiusProperty, value);
	public static CornerRadius GetCornerRadius(DependencyObject element) => (CornerRadius)element.GetValue(CornerRadiusProperty);

	/// <summary>
	/// Placeholder（占位提示）。
	/// 定义备用：为原生 TextBox 提供占位水印需额外的值管理/模板接线，故此处仅暴露契约，接线后续按需进行。
	/// </summary>
	public static readonly DependencyProperty PlaceholderProperty =
		DependencyProperty.RegisterAttached(
			"Placeholder",
			typeof(string),
			typeof(Controls),
			new FrameworkPropertyMetadata(null));

	public static void SetPlaceholder(DependencyObject element, string value) => element.SetValue(PlaceholderProperty, value);
	public static string GetPlaceholder(DependencyObject element) => (string)element.GetValue(PlaceholderProperty);

	/// <summary>
	/// 强调（主按钮）开关。
	/// 已接线到隐式 Button 样式的 Style.Triggers：置 True 时背景/边框切换为 AccentBrush。
	/// </summary>
	public static readonly DependencyProperty AccentProperty =
		DependencyProperty.RegisterAttached(
			"Accent",
			typeof(bool),
			typeof(Controls),
			new PropertyMetadata(false));

	public static void SetAccent(DependencyObject element, bool value) => element.SetValue(AccentProperty, value);
	public static bool GetAccent(DependencyObject element) => (bool)element.GetValue(AccentProperty);
}