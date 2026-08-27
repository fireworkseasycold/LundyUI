using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LundyUI.WPF.CustomControls;

/// <summary>
/// 触摸友好的垂直分隔条（TouchVerticalSplitter）。
///
/// 适配工业触摸屏的拖动需求，解决原生 GridSplitter 手指难以命中、拖拽漂移的痛点：
///   · 视觉：仅一根细线 + 居中圆形手柄（分隔条所在的 Auto 列即一个透明分隔槽，不遮挡两侧内容）
///   · 触摸热区：控件内部 Width = 2 * <see cref="TouchHotExpand"/>（默认 40px），分隔条所在
///     Auto 列就按该宽度形成透明分隔槽，整槽即命中区（约 40px，满足 ≥20px 手指目标规范），
///     且不覆盖任何面板 → 不会干扰相邻面板的滚动条等交互
///   · 拖动路径：鼠标走原生 GridSplitter 逻辑（强制 Stretch 对齐保证 resize 目标是前后两列）；
///     触摸走 Manipulation 事件（IsManipulationEnabled），解决触摸转鼠标导致的拖动漂移/断触
///   · 交互反馈：按下/拖动时 <see cref="IsDraggingVisual"/>=true，模板据此高亮线条与手柄
///   · 防拖没：拖动时遵循相邻列的 MinWidth/MaxWidth，业务列请设置 MinWidth
///
/// 用法（分隔条所在列必须为 Auto，相邻业务列设置 MinWidth）：
///   <Grid.ColumnDefinitions>
///       <ColumnDefinition Width="*"  MinWidth="120"/>
///       <ColumnDefinition Width="Auto"/>
///       <ColumnDefinition Width="2*" MinWidth="200"/>
///   </Grid.ColumnDefinitions>
///   <lui:TouchVerticalSplitter Grid.Column="1"/>
/// </summary>
public class TouchVerticalSplitter : System.Windows.Controls.GridSplitter
{
	static TouchVerticalSplitter()
	{
		DefaultStyleKeyProperty.OverrideMetadata(
			typeof(TouchVerticalSplitter),
			new FrameworkPropertyMetadata(typeof(TouchVerticalSplitter)));
	}

	/// <summary>触摸拖拽累计的水平位移（用于 DragCompleted 事件回写）。</summary>
	private double _touchDeltaX;

	public TouchVerticalSplitter()
	{
		ResizeDirection = GridResizeDirection.Columns;
		DragIncrement = 4;
		// GridSplitter 会把 Columns 方向的分隔条对齐设为 Right，导致原生拖动/自定义 resize 目标
		// 变成「前一列 + 自身列」而非「前后两列」；强制 Stretch 保证目标是分隔条前后两列。
		HorizontalAlignment = HorizontalAlignment.Stretch;
		IsManipulationEnabled = true;
		Focusable = true;
		UpdateHotZone();

		// 触摸路径：由 Manipulation 事件驱动拖拽，稳定且跟手
		ManipulationStarted += OnManipulationStarted;
		ManipulationDelta += OnManipulationDelta;
		ManipulationCompleted += OnManipulationCompleted;
		ManipulationInertiaStarting += (_, e) => e.Handled = true; // 抬起即停，不做惯性滑动

		// 触摸捕获/释放兜底：个别工业触摸屏存在断触，做显式捕获
		TouchDown += (_, e) => CaptureTouch(e.TouchDevice);
		TouchLeave += (_, e) => ReleaseTouchCapture(e.TouchDevice);

		// 鼠标路径（桌面）：复用原生 GridSplitter 拖动；统一驱动 IsDraggingVisual 高亮
		PreviewMouseLeftButtonDown += (_, _) => SetCurrentValue(IsDraggingVisualProperty, true);
		LostMouseCapture += (_, _) => SetCurrentValue(IsDraggingVisualProperty, false);
	}

	// ===== 触摸拖拽 =====

	private void OnManipulationStarted(object? sender, ManipulationStartedEventArgs e)
	{
		e.Handled = true;
		_touchDeltaX = 0;
		SetCurrentValue(IsDraggingVisualProperty, true);
	}

	private void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
	{
		e.Handled = true;
		double delta = e.DeltaManipulation.Translation.X;
		if (delta == 0)
		{
			return;
		}
		_touchDeltaX += delta;
		ApplyResize(delta);
	}

	private void OnManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
	{
		e.Handled = true;
		SetCurrentValue(IsDraggingVisualProperty, false);
		// 与鼠标路径一致：拖拽结束后通知外部（如 MainWindow 的 DragCompleted 回写 ViewModel）
		RaiseEvent(new DragCompletedEventArgs(_touchDeltaX, 0, false)
		{
			RoutedEvent = DragCompletedEvent,
			Source = this
		});
	}

	/// <summary>
	/// 把水平位移应用到分隔条**前后两列**（分隔条所在列必须是 Auto 透明分隔槽，
	/// 固定取 index-1 / index+1，不依赖 HorizontalAlignment——GridSplitter 会把 Columns
	/// 方向的分隔条对齐设为 Right，按对齐判定会误取「自身列」）。
	/// </summary>
	private void ApplyResize(double deltaX)
	{
		if (Parent is not Grid grid)
		{
			return;
		}
		int index = Grid.GetColumn(this);
		var defs = grid.ColumnDefinitions;
		if (index < 1 || index >= defs.Count - 1)
		{
			return;
		}

		var left = defs[index - 1];
		var right = defs[index + 1];

		double leftMin = left.MinWidth, rightMin = right.MinWidth;
		double leftMax = double.IsInfinity(left.MaxWidth) ? double.MaxValue : left.MaxWidth;
		double rightMax = double.IsInfinity(right.MaxWidth) ? double.MaxValue : right.MaxWidth;

		double delta = deltaX;
		double leftTarget = left.ActualWidth + delta;
		if (leftTarget < leftMin) { delta = leftMin - left.ActualWidth; }
		else if (leftTarget > leftMax) { delta = leftMax - left.ActualWidth; }

		double rightTarget = right.ActualWidth - delta;
		if (rightTarget < rightMin) { delta = right.ActualWidth - rightMin; }
		else if (rightTarget > rightMax) { delta = right.ActualWidth - rightMax; }

		left.Width = new GridLength(left.ActualWidth + delta, GridUnitType.Pixel);
		right.Width = new GridLength(right.ActualWidth - delta, GridUnitType.Pixel);
	}

	// ===== 可调属性 =====

	/// <summary>视觉细线宽度（px）。</summary>
	public static readonly DependencyProperty VisualLineWidthProperty =
		DependencyProperty.Register(nameof(VisualLineWidth), typeof(double), typeof(TouchVerticalSplitter),
			new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
	public double VisualLineWidth
	{
		get => (double)GetValue(VisualLineWidthProperty);
		set => SetValue(VisualLineWidthProperty, value);
	}

	/// <summary>触摸热区半宽（px）：分隔条所在 Auto 列即透明分隔槽，槽宽 = 2 * TouchHotExpand（默认 40px），整槽可命中。</summary>
	public static readonly DependencyProperty TouchHotExpandProperty =
		DependencyProperty.Register(nameof(TouchHotExpand), typeof(double), typeof(TouchVerticalSplitter),
			new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, OnTouchHotExpandChanged));
	public double TouchHotExpand
	{
		get => (double)GetValue(TouchHotExpandProperty);
		set => SetValue(TouchHotExpandProperty, value);
	}

	private static void OnTouchHotExpandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((TouchVerticalSplitter)d).UpdateHotZone();

	private void UpdateHotZone()
	{
		// 分隔条宽度 = 透明分隔槽宽度（即触摸热区总宽）
		Width = 2 * Math.Max(0, TouchHotExpand);
	}

	/// <summary>拖拽手柄圆点直径（px）。</summary>
	public static readonly DependencyProperty HandleSizeProperty =
		DependencyProperty.Register(nameof(HandleSize), typeof(double), typeof(TouchVerticalSplitter),
			new FrameworkPropertyMetadata(18.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
	public double HandleSize
	{
		get => (double)GetValue(HandleSizeProperty);
		set => SetValue(HandleSizeProperty, value);
	}

	/// <summary>视觉细线高度（px）。</summary>
	public static readonly DependencyProperty HandleLineHeightProperty =
		DependencyProperty.Register(nameof(HandleLineHeight), typeof(double), typeof(TouchVerticalSplitter),
			new FrameworkPropertyMetadata(80.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
	public double HandleLineHeight
	{
		get => (double)GetValue(HandleLineHeightProperty);
		set => SetValue(HandleLineHeightProperty, value);
	}

	/// <summary>按下/拖动高亮状态（控件内部维护，供模板触发器使用；与基类只读 Thumb.IsDragging 区分命名）。</summary>
	public static readonly DependencyProperty IsDraggingVisualProperty =
		DependencyProperty.Register(nameof(IsDraggingVisual), typeof(bool), typeof(TouchVerticalSplitter),
			new PropertyMetadata(false));
	public bool IsDraggingVisual
	{
		get => (bool)GetValue(IsDraggingVisualProperty);
		set => SetValue(IsDraggingVisualProperty, value);
	}
}
