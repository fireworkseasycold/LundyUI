using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LundyUI.WPF.CustomControls;

namespace LundyUI.Demo;

/// <summary>
/// MenuControl 四向停靠演示：Left/Right 竖条、Top/Bottom 横条，
/// 子菜单 Popup 依据 MenuPlacement 自动向窗口内侧弹出。
/// </summary>
public partial class MenuPlacementDemoWindow : Window
{
    /// <summary>层级菜单数据：只提供分类头与无分组独立项，子项放 Children。</summary>
    public ObservableCollection<MenuNode> MenuList { get; } = new();

    private readonly MenuControl _menu = new();
    private readonly Border _content = new();

    public MenuPlacementDemoWindow()
    {
        InitializeComponent();
        DataContext = this;
        BuildDemoMenu();

        PlacementBox.ItemsSource = new List<string> { "Left（左侧）", "Right（右侧）", "Top（顶部）", "Bottom（底部）" };
        PlacementBox.SelectedIndex = 0;

        _menu.Command = new DemoMenuCommand(node =>
        {
            if (node?.Tag is string page)
            {
                SelectedPageText.Text = $"当前页面：{page}";
            }
        });

        RebuildShell((MenuPlacement)PlacementBox.SelectedIndex);
    }

    private void BuildDemoMenu()
    {
        MenuList.Add(new MenuNode { Name = "数据看板", IsCategory = true, Icon = "chart-bar" });
        MenuList[0].Children.Add(new MenuNode { Name = "生产概览", Icon = "factory", Tag = "Dashboard" });
        MenuList[0].Children.Add(new MenuNode { Name = "实时报警", Icon = "bell-alert", Tag = "Alarm" });
        MenuList[0].Children.Add(new MenuNode { Name = "设备状态", Icon = "monitor", Tag = "Machine" });

        MenuList.Add(new MenuNode { Name = "系统", IsCategory = true, Icon = "cog" });
        MenuList[1].Children.Add(new MenuNode { Name = "用户管理", Icon = "account", Tag = "User" });
        MenuList[1].Children.Add(new MenuNode { Name = "日志查询", Icon = "file-document", Tag = "Log" });
        MenuList[1].Children.Add(new MenuNode { Name = "参数设置", Icon = "tune", Tag = "Settings" });

        // 无分组的独立项
        MenuList.Add(new MenuNode { Name = "关于", Icon = "information-outline", Tag = "About" });
    }

    /// <summary>依据停靠方向重建 Shell 行列结构并把菜单放入对应槽位。</summary>
    private void RebuildShell(MenuPlacement placement)
    {
        ShellGrid.Children.Clear();
        ShellGrid.RowDefinitions.Clear();
        ShellGrid.ColumnDefinitions.Clear();
        bool horizontal = placement is MenuPlacement.Top or MenuPlacement.Bottom;

        // 菜单条尺寸：竖条 240 宽 / 横条 48 高
        var menuBar = new Border
        {
            Child = _menu,
            Width = horizontal ? double.NaN : 240,
            Height = horizontal ? 48 : double.NaN,
        };

        if (horizontal)
        {
            if (placement == MenuPlacement.Top)
            {
                ShellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            ShellGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            if (placement == MenuPlacement.Bottom)
            {
                ShellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
        }
        else
        {
            if (placement == MenuPlacement.Left)
            {
                ShellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }
            ShellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (placement == MenuPlacement.Right)
            {
                ShellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }
        }

        _content.Background = (System.Windows.Media.Brush)FindResource("ContentBackBrush");

        int menuRow = placement switch
        {
            MenuPlacement.Bottom => ShellGrid.RowDefinitions.Count - 1,
            _ => 0,
        };
        int menuCol = placement switch
        {
            MenuPlacement.Right => ShellGrid.ColumnDefinitions.Count - 1,
            _ => 0,
        };

        Grid.SetRow(menuBar, horizontal ? menuRow : 0);
        Grid.SetColumn(menuBar, horizontal ? 0 : menuCol);
        Grid.SetRowSpan(menuBar, horizontal ? 1 : ShellGrid.RowDefinitions.Count);
        Grid.SetColumnSpan(menuBar, horizontal ? ShellGrid.ColumnDefinitions.Count : 1);

        Grid.SetRow(_content, horizontal ? ShellGrid.RowDefinitions.Count - 1 : 0);
        Grid.SetColumn(_content, horizontal ? 0 : ShellGrid.ColumnDefinitions.Count - 1);
        Grid.SetRowSpan(_content, horizontal ? 1 : ShellGrid.RowDefinitions.Count);
        Grid.SetColumnSpan(_content, horizontal ? ShellGrid.ColumnDefinitions.Count : 1);

        ShellGrid.Children.Add(menuBar);
        ShellGrid.Children.Add(_content);
    }

    private void OnPlacementChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlacementBox.SelectedItem is null || ShellGrid is null)
        {
            return;
        }
        RebuildShell((MenuPlacement)PlacementBox.SelectedIndex);
    }

    /// <summary>演示用命令：回调携带选中的 MenuNode。</summary>
    private sealed class DemoMenuCommand : ICommand
    {
        private readonly Action<MenuNode?> _execute;

        public DemoMenuCommand(Action<MenuNode?> execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter as MenuNode);
    }
}
