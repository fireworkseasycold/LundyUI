using System;
using System.Windows;
using System.Windows.Controls;

namespace LundyUI.WPF.CustomControls;

/// <summary>
/// LundyUI 日期时间选择控件（替代 HandyControl DateTimePicker）。
/// 行为兼容：SelectedDateTime（双向绑定）/ DateTimeFormat。
/// 弹出面板：日历选日期 + 下拉选时分秒，点“确定”回写 SelectedDateTime。
/// </summary>
public partial class DateTimePicker : UserControl
{
	/// <summary>
	/// 日历允许显示的最大年份。
	/// WPF 原生 Calendar 在切换到“年视图/月视图”时会在内部构造 new DateTime(year, month, day)，
	/// 当显示年份接近 DateTime.MaxValue.Year(9999) 时会因年份越界抛出
	/// “Year, Month, and Day parameters describe an un-representable DateTime”异常。
	/// 通过 DisplayDateEnd 将年份上限收窄到 9998，保证内部计算不会越过 9999。
	/// </summary>
	private static readonly int SafeMaxYear = DateTime.MaxValue.Year - 1;

	public static readonly DependencyProperty SelectedDateTimeProperty =
		DependencyProperty.Register(nameof(SelectedDateTime), typeof(DateTime?), typeof(DateTimePicker),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateTimeChanged));

	public static readonly DependencyProperty DateTimeFormatProperty =
		DependencyProperty.Register(nameof(DateTimeFormat), typeof(string), typeof(DateTimePicker),
			new PropertyMetadata("yyyy-MM-dd HH:mm:ss", OnDateTimeFormatChanged));

	private const string DefaultFormat = "yyyy-MM-dd HH:mm:ss";

	private DateTime? _draft;
	private bool _confirmed;

	public DateTimePicker()
	{
		InitializeComponent();
		PickerPopup.PlacementTarget = this;
		// 收紧日历可显示范围，规避 WPF Calendar 年份面板构造非法 DateTime 的框架缺陷
		DateCalendar.DisplayDateStart = new DateTime(1, 1, 1);
		DateCalendar.DisplayDateEnd = new DateTime(SafeMaxYear, 12, 31);
		BuildTimeItems();
		ApplyFormat();
		UpdateDisplay();
	}

	public DateTime? SelectedDateTime
	{
		get => (DateTime?)GetValue(SelectedDateTimeProperty);
		set => SetValue(SelectedDateTimeProperty, value);
	}

	public string DateTimeFormat
	{
		get => (string)GetValue(DateTimeFormatProperty);
		set => SetValue(DateTimeFormatProperty, value);
	}

	private static void OnSelectedDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((DateTimePicker)d).UpdateDisplay();

	private static void OnDateTimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var ctl = (DateTimePicker)d;
		ctl.ApplyFormat();
		ctl.UpdateDisplay();
	}

	private string CurrentFormat => string.IsNullOrWhiteSpace(DateTimeFormat) ? DefaultFormat : DateTimeFormat;

	private void BuildTimeItems()
	{
		for (int i = 0; i < 24; i++) HourBox.Items.Add(i.ToString("00"));
		for (int i = 0; i < 60; i++)
		{
			string s = i.ToString("00");
			MinuteBox.Items.Add(s);
			SecondBox.Items.Add(s);
		}
	}

	/// <summary>依据格式是否含 H/h、m、s 决定时分秒选择器的显隐。</summary>
	private void ApplyFormat()
	{
		string fmt = CurrentFormat;
		bool hasHour = fmt.Contains("H") || fmt.Contains("h");
		bool hasMinute = fmt.Contains("m");
		bool hasSecond = fmt.Contains("s");
		HourBox.Visibility = hasHour ? Visibility.Visible : Visibility.Collapsed;
		MinuteBox.Visibility = hasMinute ? Visibility.Visible : Visibility.Collapsed;
		SecondSep.Visibility = hasSecond ? Visibility.Visible : Visibility.Collapsed;
		SecondBox.Visibility = hasSecond ? Visibility.Visible : Visibility.Collapsed;
	}

	private void UpdateDisplay()
	{
		if (DisplayBox == null) return;
		DisplayBox.Text = SelectedDateTime is DateTime dt ? dt.ToString(CurrentFormat) : string.Empty;
	}

	private void OnToggleClick(object sender, RoutedEventArgs e)
	{
		if (PickerPopup.IsOpen)
		{
			PickerPopup.IsOpen = false;
			return;
		}

		DateTime baseTime = SelectedDateTime ?? DateTime.Now;
		// 若绑定值落在日期上限附近（WPF Calendar 年视图内部会推导更晚年份导致越界），回退到当前时间
		if (baseTime.Year > SafeMaxYear)
		{
			baseTime = DateTime.Now;
		}
		_draft = baseTime;
		_confirmed = false;

		DateCalendar.SelectedDatesChanged -= OnDateSelectedChanged;
		DateCalendar.DisplayDate = baseTime;
		DateCalendar.SelectedDate = baseTime;
		DateCalendar.SelectedDatesChanged += OnDateSelectedChanged;

		SelectTimeItem(HourBox, baseTime.Hour);
		SelectTimeItem(MinuteBox, baseTime.Minute);
		SelectTimeItem(SecondBox, baseTime.Second);

		PickerPopup.IsOpen = true;
	}

	private void SelectTimeItem(ComboBox box, int value)
	{
		box.SelectionChanged -= OnTimeSelectionChanged;
		box.SelectedIndex = value >= 0 && value < box.Items.Count ? value : -1;
		box.SelectionChanged += OnTimeSelectionChanged;
	}

	private void OnDateSelectedChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (DateCalendar.SelectedDate is DateTime date && _draft is DateTime d)
		{
			_draft = new DateTime(date.Year, date.Month, date.Day, d.Hour, d.Minute, d.Second);
		}
	}

	private void OnTimeSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_draft is not DateTime d) return;
		_draft = new DateTime(d.Year, d.Month, d.Day, GetIndex(HourBox), GetIndex(MinuteBox), GetIndex(SecondBox));
	}

	private static int GetIndex(ComboBox box) => box.SelectedIndex >= 0 ? box.SelectedIndex : 0;

	private void OnConfirmClick(object sender, RoutedEventArgs e)
	{
		_confirmed = true;
		if (_draft is DateTime d) SelectedDateTime = d;
		PickerPopup.IsOpen = false;
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		// 取消（点击外部关闭）时不改动绑定值，恢复显示为原值
		if (!_confirmed) UpdateDisplay();
	}
}