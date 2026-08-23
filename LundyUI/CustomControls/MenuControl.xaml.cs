using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LundyUI.WPF.CustomControls;

/// <summary>
/// LundyUI 自定义菜单控件（零业务依赖）。
/// 支持分类头展开/折叠、子项选中高亮、折叠态仅显示图标。
/// 数据模型使用 <see cref="MenuNode"/>，可直接在代码中 new 硬编码，也可由宿主从配置转换。
/// </summary>
public partial class MenuControl : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(MenuControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(MenuNode), typeof(MenuControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MenuExpandProperty =
        DependencyProperty.Register(nameof(MenuExpand), typeof(Visibility), typeof(MenuControl),
            new PropertyMetadata(Visibility.Visible));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(MenuControl),
            new PropertyMetadata(null));

    public static readonly RoutedEvent SelectionChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MenuControl));

    public MenuControl()
    {
        InitializeComponent();
    }

    /// <summary>菜单项数据源（建议 ObservableCollection&lt;MenuNode&gt;）。</summary>
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>当前选中节点（分类头点击会自动清空）。</summary>
    public MenuNode? SelectedItem
    {
        get => (MenuNode?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>展开/折叠状态（绑定宿主 ViewModel 的同名属性）。</summary>
    public Visibility MenuExpand
    {
        get => (Visibility)GetValue(MenuExpandProperty);
        set => SetValue(MenuExpandProperty, value);
    }

    /// <summary>子项选中时执行的命令（参数为选中 MenuNode.Tag）。</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>子项选中时触发（分类头展开/折叠不触发）。</summary>
    public event RoutedEventHandler SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    private void OnMenuListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MenuListBox.SelectedItem is not MenuNode node)
        {
            return;
        }

        if (node.IsCategory)
        {
            // 分类头：切换展开/折叠，并同步子节点可见性；不触发外部命令/事件
            node.IsExpanded = !node.IsExpanded;
            foreach (MenuNode child in node.Children)
            {
                child.MenuShow = node.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
            // 异步清空选中，避免分类头高亮残留
            Dispatcher.BeginInvoke(() => SelectedItem = null);
            return;
        }

        // 普通菜单项：执行命令并冒泡事件
        if (Command?.CanExecute(node.Tag) == true)
        {
            Command.Execute(node.Tag);
        }
        RaiseEvent(new RoutedEventArgs(SelectionChangedEvent, this));
    }
}
