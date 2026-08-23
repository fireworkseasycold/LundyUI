using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using LundyUI.WPF.CustomControls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Data;

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
		InitDemoMenu();
		BuildSwatches();
	}

	/// <summary>演示菜单数据：可直接 new 硬编码，不依赖 JSON 配置。</summary>
	public ObservableCollection<MenuNode> DemoMenu { get; } = new ObservableCollection<MenuNode>();

	/// <summary>第 11 节综合样张的硬编码设备数据。</summary>
	public ObservableCollection<PaletteMachine> Machines { get; } = new ObservableCollection<PaletteMachine>
	{
		new PaletteMachine { Name = "M1 六轴机器人", Status = "运行", Temp = "42.5" },
		new PaletteMachine { Name = "上料台", Status = "运行", Temp = "31.0" },
		new PaletteMachine { Name = "拧紧轴组", Status = "告警", Temp = "—" },
		new PaletteMachine { Name = "移载机", Status = "停止", Temp = "—" },
	};

	/// <summary>第 4 节 DataGrid 演示用可编辑数据源：数据项为带属性的 PaletteRow，
	/// 列绑定 {Binding Name}/{Binding Value} 带 Path，单元格编辑可双向写回。
	/// （原 x:Array 的 string 不可编辑，且裸 {Binding} 无 Path，进入编辑会抛
	/// "双向绑定需要 Path 或 XPath"）。</summary>
	public ObservableCollection<PaletteRow> EditableRows { get; } = new ObservableCollection<PaletteRow>
	{
		new PaletteRow { Name = "数据行一", Value = "100" },
		new PaletteRow { Name = "数据行二", Value = "200" },
		new PaletteRow { Name = "数据行三", Value = "300" },
	};

	private void InitDemoMenu()
	{
		// 注意：图标一律走 Icon（M D I 图标名，IconGlyphConverter 解析字形）。
		//       切勿把单个中文字符塞进 ImagePath——PathToImageConverter 会把它当文件路径解码
		//       并抛 System.IO.FileNotFoundException（本例根因，已修复）。图标名已在字形表验证。
		var data = new MenuNode
		{
			Name = "数据看板",
			IsCategory = true,
			IsExpanded = true,
			Icon = "chart-bar"
		};
		data.Children.Add(new MenuNode { Name = "生产概览", Icon = "factory", Tag = "Dashboard" });
		data.Children.Add(new MenuNode { Name = "实时报警", Icon = "bell-alert", Tag = "Alarm" });

		var system = new MenuNode
		{
			Name = "系统",
			IsCategory = true,
			IsExpanded = true,
			Icon = "cog"
		};
		system.Children.Add(new MenuNode { Name = "用户管理", Icon = "account", Tag = "User" });
		system.Children.Add(new MenuNode { Name = "日志查询", Icon = "file-document", Tag = "Log" });

		DemoMenu.Add(data);
		foreach (var child in data.Children)
		{
			child.ConfigMenuShow = child.MenuShow;
			DemoMenu.Add(child);
		}
		DemoMenu.Add(system);
		foreach (var child in system.Children)
		{
			child.ConfigMenuShow = child.MenuShow;
			DemoMenu.Add(child);
		}
	}

	/// <summary>构建第 1 节主题色板小色块：SetResourceReference 等价 DynamicResource，换肤实时跟随。</summary>
	private void BuildSwatches()
	{
		AddSwatches(StructureSwatches, "WindowBackBrush", "ContentBackBrush", "PanelBackBrush", "PanelLightBrush",
			"CardBackBrush", "LogPanelBackBrush", "TitleBarBackBrush", "StatusBarBackBrush", "RightPanelBackBrush",
			"CardHeaderBackBrush", "MenuItemBackBrush", "MenuItemHoverBrush", "MenuItemSelectedBrush",
			"PrimaryBrush", "AccentBrush");
		AddSwatches(SemanticSwatches, "InfoBackBrush", "WarnBackBrush", "SuccessBackBrush", "DangerBackBrush", "TerminalBackBrush");
		AddSwatches(TextBorderSwatches, "TextDarkBrush", "TextSecondaryBrush", "TextMutedBrush", "TextDisabledBrush",
			"TextOnAccentBrush", "BorderNormalBrush", "SplitterBrush");
		AddSwatches(StatusSwatches, "DangerBrush", "SuccessBrush", "StatusRedBrush", "StatusOrangeBrush",
			"StatusGreenBrush", "StatusBlueBrush", "StatusPurpleBrush", "StatusGrayBrush");
	}

	private static void AddSwatches(Panel host, params string[] keys)
	{
		foreach (string key in keys)
			host.Children.Add(BuildSwatch(key));
	}

	private static FrameworkElement BuildSwatch(string key)
	{
		var panel = new StackPanel { Margin = new Thickness(6), ToolTip = key };
		var box = new Border
		{
			Width = 28,
			Height = 28,
			CornerRadius = new CornerRadius(3),
			BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x80, 0x80, 0x80)),
			BorderThickness = new Thickness(1),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		box.SetResourceReference(Border.BackgroundProperty, key);

		var label = new TextBlock
		{
			Text = key,
			FontSize = 8,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 3, 0, 0),
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		label.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

		panel.Children.Add(box);
		panel.Children.Add(label);
		return panel;
	}

	/// <summary>
	/// 点击展示格弹出放大预览：克隆格子内容为真实尺寸实例（XamlWriter 保留 DynamicResource
	/// 表达式，克隆体与 App 资源环境联通 -> 主题切换实时跟随；DatePicker/Calendar 等展开的
	/// Popup 弹层为真实大小，可直接交互验证，不再受格子尺寸限制）。
	/// </summary>
	private void OnCellClick(object sender, RoutedEventArgs e)
	{
		// sender 为本次点击的 DemoCell（其 Click 为 RoutedEventHandler）
		if (sender is not DemoCell cell) return;

		string? title = string.IsNullOrWhiteSpace(cell.Title) ? null : cell.Title.Trim();
		OverlayTitle.Text = (string.IsNullOrEmpty(title) ? "样式预览" : title) + "（点遮罩或右上角 X 关闭）";

		if (cell.Content is FrameworkElement content)
			OverlayHost.Content = CloneForPreview(content);
		Overlay.Visibility = Visibility.Visible;
	}

	/// <summary>
	/// 克隆格子内容为真实尺寸预览实例：移除 key 名标题行，保留各控件显式尺寸
	/// （Calendar/DatePicker/ToolBar 内下拉框等定宽控件不再被拉伸）；
	/// Width=NaN 的拉伸型控件由预览宿主有限宽撑开（Slider/ProgressBar 等）。
	/// 克隆失败（罕见，如命名冲突）时回退原格子等比放大，保证预览不中断。
	/// </summary>
	private static FrameworkElement CloneForPreview(FrameworkElement content)
	{
		// 自定义 UserControl（内含命名元素 + 资源字典，如 DateTimePicker 的 "DisplayBox"、
		// LoadingCircle 的 "Arc"、Pagination 的 "PageButtonStyle"）无法用 XamlWriter/XamlReader
		// 往返序列化：其构造函数 InitializeComponent 已注册名称/键，二次解析会重复注册抛异常。
		// 对这类内容直接输出可交互的原比例矢量快照，避免任何运行时异常。
		if (ContainsUserControl(content))
		{
			return new Rectangle
			{
				Width = Math.Max(content.RenderSize.Width, 1),
				Height = Math.Max(content.RenderSize.Height, 1),
				Fill = new VisualBrush(content) { Stretch = Stretch.Fill }
			};
		}

		try
		{
			// XamlWriter.Save 会为带名称的对象写出 x:Name；二次解析会在新名称作用域中因
			// 重复注册抛异常，先剥离所有 x:Name。
			// DataGrid 的 ItemsSource 若是 RelativeSource 绑定，往返序列化不可靠，可能
			// 直接抛异常导致退回静态快照；序列化前临时置空，随后恢复，克隆体由
			// CopyDataGridColumns 重新注入原集合（保持可编辑）。
			DataGrid? dg = content as DataGrid;
			IEnumerable? savedItems = null;
			if (dg != null) { savedItems = dg.ItemsSource; dg.ItemsSource = null; }
			string xaml;
			try
			{
				xaml = Regex.Replace(XamlWriter.Save(content), @"\sx:Name\s*=\s*""[^""]*""", " ");
			}
			finally
			{
				if (dg != null) dg.ItemsSource = savedItems;
			}
			var clone = (FrameworkElement)XamlReader.Parse(xaml);
			// 历史遗留：若内容为 StackPanel 且首子元素是 key 名 TextBlock，则移除
			if (clone is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is TextBlock)
				sp.Children.RemoveAt(0);
			CopyDataGridColumns(content, clone);
			RepairPopupBindings(clone);
			return clone;
		}
		catch
		{
			// 回退：原内容等比放大（VisualBrush 矢量缩放不模糊）
			return new Rectangle
			{
				Width = Math.Max(content.RenderSize.Width, 1),
				Height = Math.Max(content.RenderSize.Height, 1),
				Fill = new VisualBrush(content) { Stretch = Stretch.Fill }
			};
		}
	}

	/// <summary>是否在视觉树中检测到 UserControl（这类控件无法安全序列化往返）。</summary>
	private static bool ContainsUserControl(DependencyObject root)
	{
		if (root is UserControl) return true;
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
			if (ContainsUserControl(VisualTreeHelper.GetChild(root, i))) return true;
		return false;
	}

	/// <summary>
	/// XamlWriter.Save 不序列化 DataGrid.Columns（列集合），克隆后按原位置手动复制列，
	/// 保证放大预览能看到列头/列宽（ListView 的 GridView 列 Save 会保留，无需处理）。
	/// 同时支持内容直接就是 DataGrid（如第 4 节"数据表格"格），否则放大后只剩空表格框。
	/// </summary>
	private static void CopyDataGridColumns(FrameworkElement source, FrameworkElement target)
	{
		// 内容直接是 DataGrid：列集合不被序列化，逐列复制；同时共享原格已求值的
		// 可编辑集合（克隆体的 RelativeSource 绑定跨树解析不可靠，直接赋值保证能编辑）
		if (source is DataGrid srcDgDirect && target is DataGrid dstDgDirect)
		{
			dstDgDirect.ItemsSource = srcDgDirect.ItemsSource;
			foreach (DataGridColumn col in srcDgDirect.Columns)
			{
				try { dstDgDirect.Columns.Add((DataGridColumn)XamlReader.Parse(XamlWriter.Save(col))); }
				catch { /* 单列复制失败忽略：行样式仍可见 */ }
			}
			return;
		}
		if (source is not StackPanel ss || target is not StackPanel ts) return;
		int tIdx = 0;
		for (int sIdx = 0; sIdx < ss.Children.Count && tIdx < ts.Children.Count; sIdx++)
		{
			// 原格第一个 child 若是 key 名 TextBlock（克隆时已移除），跳过对齐
			if (sIdx == 0 && ss.Children[sIdx] is TextBlock) continue;
			if (ss.Children[sIdx] is DataGrid srcDg && ts.Children[tIdx] is DataGrid dstDg)
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

	/// <summary>
	/// 克隆会剥离所有 x:Name，导致 Popup 依赖的 ElementName 绑定（IsOpen/PlacementTarget）断裂。
	/// 在克隆树内重建：找到 Popup 与其对应的 ToggleButton，用 Source 绑定 + 直接赋值恢复弹出行为，
	/// 使放大预览中"切换弹出层"仍可交互。
	/// </summary>
	private static void RepairPopupBindings(FrameworkElement root)
	{
		var popup = FindFirstDescendant<Popup>(root);
		if (popup == null) return;
		var toggle = FindFirstDescendant<ToggleButton>(root);
		if (toggle == null) return;
		popup.SetBinding(Popup.IsOpenProperty,
			new Binding(nameof(ToggleButton.IsChecked)) { Source = toggle });
		popup.PlacementTarget = toggle;
	}

	/// <summary>按视觉树深度优先查找第一个匹配类型的后代（含自身）。</summary>
	private static T? FindFirstDescendant<T>(DependencyObject root) where T : DependencyObject
	{
		if (root is T match) return match;
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var found = FindFirstDescendant<T>(VisualTreeHelper.GetChild(root, i));
			if (found != null) return found;
		}
		return null;
	}

	private void OnCloseOverlay(object sender, RoutedEventArgs e) => Overlay.Visibility = Visibility.Collapsed;

	/// <summary>
	/// 点击遮罩空白处关闭（点卡片内部不关闭）。
	/// </summary>
	private void OnOverlayClick(object sender, MouseButtonEventArgs e)
	{
		if (e.OriginalSource == Overlay) Overlay.Visibility = Visibility.Collapsed;
	}

	/// <summary>
	/// 演示 lui: 自定义控件 ImageViewerWindow 弹窗可用：构造示例图片项并打开查看器。
	/// </summary>
	private void OnOpenImageViewer(object sender, RoutedEventArgs e)
	{
		if (_demoImages == null)
		{
			_demoImages = new List<ImageViewItem>();
			for (int i = 1; i <= 3; i++)
			{
				_demoImages.Add(new ImageViewItem { ImagePath = CreateDemoImagePath(i) });
			}
		}
		var viewer = new ImageViewerWindow();
		viewer.ShowImages(_demoImages, 0, "LundyUI 图片查看器演示");
		viewer.Show();
	}

	private static List<ImageViewItem>? _demoImages;

	/// <summary>程序化生成一张示例位图路径（验证 ImageViewerWindow 加载、缩放、导航）。</summary>
	private static string CreateDemoImagePath(int index)
	{
		var bmp = new System.Windows.Media.Imaging.RenderTargetBitmap(640, 400, 96, 96, PixelFormats.Pbgra32);
		var canvas = new System.Windows.Controls.Canvas();
		canvas.Background = new SolidColorBrush(Color.FromRgb((byte)(30 + index * 50), 60, (byte)(120 + index * 30)));
		var tb = new TextBlock
		{
			Text = "LundyUI 示例图 " + index,
			FontSize = 32,
			Foreground = Brushes.White
		};
		tb.Measure(new Size(640, 400));
		Canvas.SetLeft(tb, (640 - tb.DesiredSize.Width) / 2);
		Canvas.SetTop(tb, (400 - tb.DesiredSize.Height) / 2);
		canvas.Children.Add(tb);
		canvas.Measure(new Size(640, 400));
		canvas.Arrange(new Rect(0, 0, 640, 400));
		bmp.Render(canvas);
		string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LundyUI.Demo");
		System.IO.Directory.CreateDirectory(dir);
		string file = System.IO.Path.Combine(dir, "demo-" + index + ".png");
		var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
		enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
		using var fs = System.IO.File.Create(file);
		enc.Save(fs);
		return file;
	}
}

/// <summary>第 11 节综合样张使用的硬编码设备记录。</summary>
public sealed class PaletteMachine
{
	public string? Name { get; set; }
	public string? Status { get; set; }
	public string? Temp { get; set; }
}

/// <summary>第 4 节 DataGrid 演示用可编辑行数据模型：与第 11 节同构，
/// 列绑定带 Path（Name/Value），单元格编辑可双向写回。</summary>
public sealed class PaletteRow
{
	public string? Name { get; set; }
	public string? Value { get; set; }
}
