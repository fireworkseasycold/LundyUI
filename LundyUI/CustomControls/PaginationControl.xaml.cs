using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// LundyUI 分页控件（替代 HandyControl Pagination）。
/// 行为兼容：MaxPageCount / PageIndex(双向) / IsJumpEnabled / DataCountPerPage(兼容绑定，不展示)，
/// 翻页动作通过 PageUpdated 事件上抛新页码（库内 FunctionEventArgs）。
/// </summary>
public partial class PaginationControl : UserControl
{
	public static readonly DependencyProperty MaxPageCountProperty =
		DependencyProperty.Register(nameof(MaxPageCount), typeof(int), typeof(PaginationControl),
			new PropertyMetadata(1, OnMaxPageCountChanged));

	public static readonly DependencyProperty PageIndexProperty =
		DependencyProperty.Register(nameof(PageIndex), typeof(int), typeof(PaginationControl),
			new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPageIndexChanged));

	public static readonly DependencyProperty IsJumpEnabledProperty =
		DependencyProperty.Register(nameof(IsJumpEnabled), typeof(bool), typeof(PaginationControl),
			new PropertyMetadata(false, OnIsJumpEnabledChanged));

	public static readonly DependencyProperty DataCountPerPageProperty =
		DependencyProperty.Register(nameof(DataCountPerPage), typeof(int), typeof(PaginationControl),
			new PropertyMetadata(20));

	/// <summary>翻页事件：参数为新页码。</summary>
	public event EventHandler<FunctionEventArgs<int>> PageUpdated = delegate { };

	public PaginationControl()
	{
		InitializeComponent();
		RebuildPageButtons();
	}

	public int MaxPageCount
	{
		get => (int)GetValue(MaxPageCountProperty);
		set => SetValue(MaxPageCountProperty, value);
	}

	public int PageIndex
	{
		get => (int)GetValue(PageIndexProperty);
		set => SetValue(PageIndexProperty, value);
	}

	public bool IsJumpEnabled
	{
		get => (bool)GetValue(IsJumpEnabledProperty);
		set => SetValue(IsJumpEnabledProperty, value);
	}

	public int DataCountPerPage
	{
		get => (int)GetValue(DataCountPerPageProperty);
		set => SetValue(DataCountPerPageProperty, value);
	}

	private static void OnMaxPageCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is PaginationControl ctl) ctl.RebuildPageButtons();
	}

	private static void OnPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is PaginationControl ctl) ctl.RebuildPageButtons();
	}

	private static void OnIsJumpEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is PaginationControl ctl) ctl.JumpPanel.Visibility = ctl.IsJumpEnabled ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <summary>重建页码按钮：当前页前后各 2 页，首尾保留，断层用省略号（-1 标记）。</summary>
	private void RebuildPageButtons()
	{
		PageButtonsHost.Items.Clear();

		int max = Math.Max(1, MaxPageCount);
		int cur = PageIndex < 1 ? 1 : (PageIndex > max ? max : PageIndex);

		foreach (int page in BuildPageSequence(cur, max))
		{
			Button btn = new Button
			{
				Content = page < 0 ? "…" : page.ToString(),
				IsEnabled = page > 0,
				Margin = new Thickness(4, 0, 0, 0),
			};
			btn.SetResourceReference(StyleProperty, "PageButtonStyle");
			if (page == cur)
			{
				// 当前页高亮（局部值覆盖样式，主题键动态跟随）
				btn.Background = (Brush)FindResource("AccentBrush");
				btn.Foreground = (Brush)FindResource("TextOnAccentBrush");
				btn.IsEnabled = false;
			}
			if (page > 0)
			{
				int target = page;
				btn.Click += (_, _) => GoToPage(target);
			}
			PageButtonsHost.Items.Add(btn);
		}

		PrevButton.IsEnabled = cur > 1;
		NextButton.IsEnabled = cur < max;
	}

	private static List<int> BuildPageSequence(int cur, int max)
	{
		List<int> pages = new List<int>();
		if (max <= 7)
		{
			for (int i = 1; i <= max; i++) pages.Add(i);
			return pages;
		}

		pages.Add(1);
		if (cur > 4) pages.Add(-1);
		for (int i = Math.Max(2, cur - 2); i <= Math.Min(max - 1, cur + 2); i++) pages.Add(i);
		if (cur < max - 3) pages.Add(-1);
		pages.Add(max);
		return pages;
	}

	private void GoToPage(int index)
	{
		if (index < 1 || index > MaxPageCount || index == PageIndex) return;
		PageIndex = index;
		PageUpdated?.Invoke(this, new FunctionEventArgs<int>(index));
	}

	private void OnPrevClick(object sender, RoutedEventArgs e) => GoToPage(PageIndex - 1);

	private void OnNextClick(object sender, RoutedEventArgs e) => GoToPage(PageIndex + 1);

	private void OnJumpClick(object sender, RoutedEventArgs e) => DoJump();

	private void OnJumpKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter) DoJump();
	}

	private void DoJump()
	{
		if (int.TryParse(JumpBox.Text, out int index))
		{
			GoToPage(index);
		}
		else
		{
			JumpBox.Clear();
		}
	}
}