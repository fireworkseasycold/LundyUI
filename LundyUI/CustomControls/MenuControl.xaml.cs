using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace LundyUI.WPF.CustomControls;

/// <summary>
/// LundyUI 自定义菜单控件（零业务依赖，Popup 展开模式）。
/// 顶层只渲染分类头与无分组的独立项；点击分类头经 Popup 弹出子菜单，
/// 弹出方向由 <see cref="MenuPlacement"/> 决定，恒指向窗口内侧（自动向内展开）。
/// 数据模型使用 <see cref="MenuNode"/>，可直接在代码中 new 硬编码，也可由宿主从配置转换。
/// </summary>
public partial class MenuControl : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(MenuControl),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(MenuNode), typeof(MenuControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static readonly DependencyProperty MenuExpandProperty =
        DependencyProperty.Register(nameof(MenuExpand), typeof(Visibility), typeof(MenuControl),
            new PropertyMetadata(Visibility.Visible));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(MenuControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty MenuPlacementProperty =
        DependencyProperty.Register(nameof(MenuPlacement), typeof(MenuPlacement), typeof(MenuControl),
            new PropertyMetadata(MenuPlacement.Left, OnMenuPlacementChanged));

    public static readonly RoutedEvent SelectionChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MenuControl));

    /// <summary>当前打开的子菜单 Popup（全局只允许一个）。</summary>
    private Popup? _activePopup;

    /// <summary>当前 Popup 对应的分类节点（关闭时复位 IsExpanded）。</summary>
    private MenuNode? _activeCategory;

    public MenuControl()
    {
        InitializeComponent();
        RefreshLayout();
        Unloaded += (_, _) => CloseActivePopup();
    }

    /// <summary>菜单项数据源（建议 ObservableCollection&lt;MenuNode&gt;）。Popup 模式下应只提供分类头与无分组独立项，子项放在分类的 Children 中。</summary>
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

    /// <summary>展开/折叠状态（绑定宿主 ViewModel 的同名属性；Collapsed 时菜单条退化为纯图标态）。</summary>
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

    /// <summary>
    /// 停靠方向：决定列表排布（Left/Right 竖条，Top/Bottom 横条）与子菜单 Popup 弹出方向（自动向内展开）。
    /// </summary>
    public MenuPlacement MenuPlacement
    {
        get => (MenuPlacement)GetValue(MenuPlacementProperty);
        set => SetValue(MenuPlacementProperty, value);
    }

    /// <summary>子项选中时触发（分类头展开/折叠不触发）。</summary>
    public event RoutedEventHandler SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    // ============== 依赖属性回调 ==============

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MenuControl control)
        {
            return;
        }

        control.CloseActivePopup();
        control.NormalizeCategories();
        control.UpdateChildSelection(control.SelectedItem);
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuControl control)
        {
            control.UpdateChildSelection(e.NewValue as MenuNode);
        }
    }

    private static void OnMenuPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MenuControl control)
        {
            return;
        }

        control.CloseActivePopup();
        control.RefreshLayout();
    }

    // ============== 布局 ==============

    /// <summary>依据 MenuPlacement 切换顶层列表排布方向、滚动条方向与分类头模板。</summary>
    private void RefreshLayout()
    {
        bool horizontal = MenuPlacement is MenuPlacement.Top or MenuPlacement.Bottom;

        MenuListBox.ItemsPanel = (ItemsPanelTemplate)Resources[horizontal ? "HorizontalItemsPanel" : "VerticalItemsPanel"];
        MenuListBox.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,
            horizontal ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled);
        MenuListBox.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty,
            horizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);

        if (Resources["MenuTemplateSelector"] is MenuTemplateSelector selector)
        {
            selector.UseHorizontalCategory = horizontal;
            MenuListBox.Items.Refresh();
        }
    }

    /// <summary>数据源就绪时将分类头复位为收起态（Popup 模式下 IsExpanded 仅表示 Popup 打开状态）。</summary>
    private void NormalizeCategories()
    {
        if (ItemsSource is not IEnumerable items)
        {
            return;
        }

        foreach (var obj in items)
        {
            if (obj is MenuNode node && node.IsCategory)
            {
                node.IsExpanded = false;
            }
        }
    }

    // ============== 选择处理 ==============

    private void OnMenuListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MenuListBox.SelectedItem is not MenuNode node)
        {
            return;
        }

        if (node.IsCategory)
        {
            // 分类头：切换子菜单 Popup；不触发外部命令/事件
            var container = MenuListBox.ItemContainerGenerator.ContainerFromItem(node) as ListBoxItem;
            ToggleCategoryPopup(node, container);
            // 异步清空选中，避免分类头高亮残留
            Dispatcher.BeginInvoke(() => SelectedItem = null);
            return;
        }

        // 无分组的独立菜单项：执行命令并冒泡事件
        ExecuteNode(node);
    }

    private void OnPopupListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox list || list.SelectedItem is not MenuNode node)
        {
            return;
        }

        if (node.IsCategory)
        {
            // 仅支持两级菜单：Popup 内的分类头不再展开
            list.SelectedItem = null;
            return;
        }

        ExecuteNode(node);
        CloseActivePopup();
        list.SelectedItem = null;
    }

    private void ExecuteNode(MenuNode node)
    {
        if (Command?.CanExecute(node.Tag) == true)
        {
            Command.Execute(node.Tag);
        }
        RaiseEvent(new RoutedEventArgs(SelectionChangedEvent, this));
        UpdateChildSelection(node);
    }

    /// <summary>依据选中节点刷新各分类头的 IsChildSelected 高亮标记。</summary>
    private void UpdateChildSelection(MenuNode? selected)
    {
        if (ItemsSource is not IEnumerable items)
        {
            return;
        }

        foreach (var obj in items)
        {
            if (obj is MenuNode category && category.IsCategory)
            {
                category.IsChildSelected = selected is not null && ContainsNode(category, selected);
            }
        }
    }

    private static bool ContainsNode(MenuNode category, MenuNode node)
    {
        foreach (var child in category.Children)
        {
            if (ReferenceEquals(child, node))
            {
                return true;
            }
        }
        return false;
    }

    // ============== 子菜单 Popup ==============

    private void ToggleCategoryPopup(MenuNode category, ListBoxItem? container)
    {
        bool isSame = ReferenceEquals(_activeCategory, category);
        CloseActivePopup();

        if (isSame || container is null || category.Children.Count == 0)
        {
            return;
        }

        var list = new ListBox
        {
            ItemsSource = category.Children,
            ItemTemplate = (DataTemplate)Resources["MenuPopupEntryTemplate"],
            ItemContainerStyle = (Style)Resources["MenuItemContainerStyle"],
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(2),
            MinWidth = 180,
            MaxHeight = 520,
            Focusable = false,
        };
        list.SetValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Item);
        list.SelectionChanged += OnPopupListSelectionChanged;

        // Popup 表面：圆角卡片 + 投影；颜色全部走主题键，禁止硬编码色值
        var surface = new Border
        {
            Child = list,
            CornerRadius = new CornerRadius(8),
            Background = (Brush)FindResource("MenuItemBackBrush"),
            BorderBrush = (Brush)FindResource("MenuPopupBorderBrush"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(14, 12, 14, 14),
            Effect = new DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 3,
                Direction = 270,
                Opacity = 0.4,
                Color = Colors.Black,
            },
        };

        var popup = new Popup
        {
            AllowsTransparency = true,
            StaysOpen = false,
            PlacementTarget = container,
            Placement = MenuPlacement switch
            {
                MenuPlacement.Right => PlacementMode.Left,
                MenuPlacement.Top => PlacementMode.Bottom,
                MenuPlacement.Bottom => PlacementMode.Top,
                _ => PlacementMode.Right,
            },
            PopupAnimation = PopupAnimation.Fade,
            Child = surface,
        };
        popup.Closed += OnActivePopupClosed;

        _activePopup = popup;
        _activeCategory = category;
        category.IsExpanded = true;
        popup.IsOpen = true;
    }

    /// <summary>Popup 因点击外部等原因关闭（StaysOpen=False）时统一复位状态。</summary>
    private void OnActivePopupClosed(object? sender, EventArgs e)
    {
        if (_activeCategory is not null)
        {
            _activeCategory.IsExpanded = false;
        }
        _activePopup = null;
        _activeCategory = null;
    }

    private void CloseActivePopup()
    {
        if (_activePopup is not null)
        {
            _activePopup.Closed -= OnActivePopupClosed;
            _activePopup.IsOpen = false;
        }
        if (_activeCategory is not null)
        {
            _activeCategory.IsExpanded = false;
        }
        _activePopup = null;
        _activeCategory = null;
    }
}
