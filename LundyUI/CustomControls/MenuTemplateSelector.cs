using System.Windows;
using System.Windows.Controls;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// 菜单模板选择器：根据 MenuNode.IsCategory 选择分类头或普通菜单项模板。
/// </summary>
public sealed class MenuTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CategoryTemplate { get; set; }

    public DataTemplate? ItemTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is MenuNode node)
        {
            return node.IsCategory ? CategoryTemplate : ItemTemplate;
        }
        return base.SelectTemplate(item, container);
    }
}
