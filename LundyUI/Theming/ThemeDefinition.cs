using System.Collections.Generic;

namespace LundyUI.Controls.Theming
{
    /// <summary>
    /// 主题定义（纯数据，零业务依赖）。
    /// 一个条目 = 一套颜色变量；由 ThemeManager 运行时构建资源字典并原位替换。
    /// 键命名约定决定资源类型：以 "Color" 结尾 → Color 资源，其余 → SolidColorBrush。
    /// </summary>
    public sealed class ThemeDefinition
    {
        /// <summary>主题名（唯一标识，切换与持久化依据）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>显示名（切换下拉框展示）</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>是否默认主题（启动时无持久化值时优先采用）</summary>
        public bool IsDefault { get; set; }

        /// <summary>颜色变量表：主题键 → 颜色值（如 "#0B1426"）；空表 = 不覆盖，落在 ThemeShared 默认</summary>
        public Dictionary<string, string> Colors { get; set; } = new();
    }
}