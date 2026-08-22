using System;
using System.Windows;
using System.Windows.Controls;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// LundyUI 日期时间选择控件（替代 HandyControl DateTimePicker）。
/// 行为兼容：SelectedDateTime（双向绑定）/ DateTimeFormat。
/// 弹出面板：日历选日期 + 下拉选时分秒，点“确定”回写 SelectedDateTime。
/// </summary>
public partial class DateTimePicker : UserControl
{
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