using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// 菜单节点数据契约（零业务依赖）。
/// 可直接 new 硬编码，也可由宿主从任意配置/JSON 转换而来。
/// 控件内部只读取 Name/ImagePath/IsCategory/IsExpanded/MenuShow/Children。
/// </summary>
public sealed class MenuNode : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string? _imagePath;
    private bool _isCategory;
    private bool _isExpanded = true;
    private Visibility _menuShow = Visibility.Visible;
    private ObservableCollection<MenuNode> _children = new();
    private object? _tag;

    /// <summary>显示文本（已由宿主完成多语言解析，控件不做翻译）。</summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>图标路径或 emoji 字符。</summary>
    public string? ImagePath
    {
        get => _imagePath;
        set => SetProperty(ref _imagePath, value);
    }

    /// <summary>是否为分类头。</summary>
    public bool IsCategory
    {
        get => _isCategory;
        set => SetProperty(ref _isCategory, value);
    }

    /// <summary>分类头展开状态；子项通过 MenuShow 受控显示。</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>Visibility 控制该节点在列表中的显示（折叠分类时子项 Collapsed）。</summary>
    public Visibility MenuShow
    {
        get => _menuShow;
        set => SetProperty(ref _menuShow, value);
    }

    /// <summary>子项原始可见性缓存：分类展开时恢复到该值（默认 Visible）。</summary>
    public Visibility ConfigMenuShow { get; set; } = Visibility.Visible;

    /// <summary>子节点集合；仅分类头使用，平铺渲染时由宿主决定顺序。</summary>
    public ObservableCollection<MenuNode> Children
    {
        get => _children;
        set => SetProperty(ref _children, value);
    }

    /// <summary>宿主可附加任意对象（如页面名、导航参数），控件不解释。</summary>
    public object? Tag
    {
        get => _tag;
        set => SetProperty(ref _tag, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
