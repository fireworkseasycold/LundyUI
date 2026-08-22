using System.Collections.Generic;

namespace LundyUI.Controls.Theming
{
    /// <summary>
    /// themes-config.json 根节点：{ "ThemeConfigs": { "Themes": [...] } }。
    /// 纯数据契约（零业务依赖），字段名与 json 键一致，供 ThemeJsonLoader 反序列化。
    /// </summary>
    public sealed class ThemeConfigContainer
    {
        /// <summary>主题配置节</summary>
        public ThemeConfigs? ThemeConfigs { get; set; }
    }

    /// <summary>主题配置节：主题列表</summary>
    public sealed class ThemeConfigs
    {
        /// <summary>主题清单（json 顺序即切换下拉框显示顺序）</summary>
        public List<ThemeConfig>? Themes { get; set; }
    }

    /// <summary>
    /// 单个主题配置条目。
    /// 键命名约定决定资源类型：以 "Color" 结尾 → Color 资源，其余 → SolidColorBrush；
    /// 键必须与 ThemeShared.xaml 完全对应，缺键会回落到 ThemeShared 默认色（掉色）。
    /// </summary>
    public sealed class ThemeConfig
    {
        /// <summary>主题名（唯一标识，切换与持久化依据）</summary>
        public string? Name { get; set; }

        /// <summary>显示名（切换下拉框展示）</summary>
        public string? DisplayName { get; set; }

        /// <summary>是否默认主题（启动时无持久化值时优先采用）</summary>
        public bool IsDefault { get; set; }

        /// <summary>颜色变量表：主题键 → 颜色值（如 "#0B1426"）</summary>
        public Dictionary<string, string>? Colors { get; set; }
    }
}