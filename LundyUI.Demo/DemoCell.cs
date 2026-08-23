using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LundyUI.Demo;

/// <summary>
/// 主题样板展示格：外层固定 300×130，内部内容由 ContentPresenter 填满。
/// 点击整格触发 Click 路由事件，用于弹出放大预览。
/// </summary>
public class DemoCell : ContentControl
{
    public DemoCell()
    {
        MouseLeftButtonUp += OnCellMouseLeftButtonUp;
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(DemoCell), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DemoCell));

    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    private void OnCellMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 由子元素（如 Button）处理的事件不再触发整格点击
        if (e.Source == this || e.OriginalSource == this)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        }
    }
}

