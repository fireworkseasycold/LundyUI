using System;
using System.Windows;
using System.Windows.Controls;
using LundyUI.Controls.Theming;

namespace LundyUI.Demo
{
    /// <summary>主窗口：顶部主题切换条 + 复刻的 ThemePaletteView 主题样板内容。</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadThemes();
            ThemeManager.Instance.ThemeChanged += OnAfterSwitch;
        }

        /// <summary>用 json 驱动注册的主题填充下拉框（由 Configs/themes/themes-config.json 决定数量与顺序）。</summary>
        private void LoadThemes()
        {
            foreach (ThemeDefinition t in ThemeManager.Instance.AvailableThemes)
                ThemeBox.Items.Add(new ComboBoxItem { Content = t.DisplayName, Tag = t.Name });
            ThemeBox.SelectedValue = ThemeManager.Instance.CurrentTheme;
        }

        /// <summary>用户在下拉框选择 → 切换主题（DynamicResource 全局即时生效，ThemePaletteView 实时跟随）。</summary>
        private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeBox.SelectedItem is ComboBoxItem item && item.Tag is string name
                && name != ThemeManager.Instance.CurrentTheme)
            {
                ThemeManager.Instance.SwitchTheme(name);
            }
        }

        /// <summary>切换完成后同步下拉框选中态与状态栏文本。</summary>
        private void OnAfterSwitch(object? sender, EventArgs e)
        {
            ThemeBox.SelectedValue = ThemeManager.Instance.CurrentTheme;
            StatusText.Text = $"当前主题：{ThemeManager.Instance.CurrentTheme}";
        }
    }
}