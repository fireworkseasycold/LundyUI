using System.Windows;
using System.Windows.Controls;

namespace LundyUI.WPF.CustomControls;

/// <summary>
/// 菜单模板选择器：根据 MenuNode.IsCategory 选择分类头或普通菜单项模板。
/// </summary>
public sealed class MenuTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CategoryTemplate { get; set; }

    /// <summary>横向菜单（Top/Bottom 停靠）使用的分类头模板；为空时回落 CategoryTemplate。</summary>
    public DataTemplate? HorizontalCategoryTemplate { get; set; }

    public DataTemplate? ItemTemplate { get; set; }

    /// <summary>是否使用横向分类头模板（由 MenuControl 依据 MenuPlacement 设置）。</summary>
    public bool UseHorizontalCategory { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is MenuNode node)
        {
            if (node.IsCategory)
            {
                return UseHorizontalCategory ? HorizontalCategoryTemplate ?? CategoryTemplate : CategoryTemplate;
            }
            return ItemTemplate;
        }
        return base.SelectTemplate(item, container);
    }
}
