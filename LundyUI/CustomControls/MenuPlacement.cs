namespace LundyUI.WPF.CustomControls;

/// <summary>
/// 菜单停靠方向。决定菜单条排布方向（竖条/横条）以及子菜单 Popup 的弹出方向——
/// 弹出方向恒指向窗口内侧（自动向内展开）：Left→右弹、Right→左弹、Top→下弹、Bottom→上弹。
/// </summary>
public enum MenuPlacement
{
    /// <summary>左侧竖条（默认），子菜单向右弹出。</summary>
    Left,

    /// <summary>右侧竖条，子菜单向左弹出。</summary>
    Right,

    /// <summary>顶部横条，子菜单向下弹出。</summary>
    Top,

    /// <summary>底部横条，子菜单向上弹出。</summary>
    Bottom,
}
