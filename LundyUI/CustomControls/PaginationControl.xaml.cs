using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// LundyUI 分页控件（替代 HandyControl Pagination）。
/// 精简版布局：◀ 当前/总数 ▶ [跳转框] GO（无首页/末页/页码按钮）。
/// 行为兼容：MaxPageCount / PageIndex(双向) / IsJumpEnabled / DataCountPerPage(兼容绑定，不展示)，
/// 翻页动作通过 PageUpdated 事件上抛新页码（库内 FunctionEventArgs）。
/// 跳转输入框仅允许 1~总页数 的整数：非数字字符输入被拦截，越界/非法值在跳转时自动忽略并回填当前页码。
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
		// 粘贴到跳转框时同样过滤非数字字符
		DataObject.AddPastingHandler(JumpBox, OnJumpBoxPasting);
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
		// 跳转框始终显示，此属性保留兼容用，不再控制可见性。
	}

	/// <summary>刷新按钮可用态 + 当前/总数文本 + 跳转框回填当前页码。</summary>
	private void RebuildPageButtons()
	{
		int max = Math.Max(1, MaxPageCount);
		int cur = PageIndex < 1 ? 1 : (PageIndex > max ? max : PageIndex);

		PrevButton.IsEnabled = cur > 1;
		NextButton.IsEnabled = cur < max;

		PageInfo.Text = $"{cur} / {max}";
		JumpBox.Text = cur.ToString();
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

	/// <summary>输入过滤：仅允许数字字符，非数字（字母/符号）自动忽略。</summary>
	private void OnJumpPreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		foreach (char c in e.Text)
		{
			if (!char.IsDigit(c))
			{
				e.Handled = true;
				return;
			}
		}
	}

	/// <summary>粘贴过滤：仅保留数字，无数字则取消粘贴。</summary>
	private void OnJumpBoxPasting(object sender, DataObjectPastingEventArgs e)
	{
		if (e.DataObject.GetDataPresent(DataFormats.UnicodeText)
		    && e.DataObject.GetData(DataFormats.UnicodeText) is string text)
		{
			string filtered = new string(text.Where(char.IsDigit).ToArray());
			if (filtered.Length == 0)
			{
				e.CancelCommand();
			}
			else
			{
				e.DataObject.SetData(DataFormats.UnicodeText, filtered);
			}
		}
		else
		{
			e.CancelCommand();
		}
	}

	/// <summary>跳转：仅接受 1~总页数 的整数；非法值（越界/非整数）自动忽略并回填当前页码。</summary>
	private void DoJump()
	{
		if (int.TryParse(JumpBox.Text, out int index) && index >= 1 && index <= MaxPageCount)
		{
			GoToPage(index);
		}
		else
		{
			JumpBox.Text = PageIndex.ToString();
		}
	}
}
