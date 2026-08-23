using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LundyUI.WPF.Theming
{
    /// <summary>
    /// themes-config.json 加载器：把配置驱动主题（ThemeConfig）映射为运行期 ThemeDefinition。
    /// 解析失败返回空集（不抛异常），由调用方（ThemeManager.LoadThemesFromConfig）决定兜底策略。
    /// </summary>
    public static class ThemeJsonLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            // json 键为 PascalCase 且与属性名一致；此处放宽大小写以容忍手工编辑差异
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// 解析 themes-config.json 为主题定义列表。
        /// 文件缺失 / 无效 json / 无主题 → 返回空表；有效条目映射为 ThemeDefinition（缺 Name 的条目丢弃）。
        /// </summary>
        public static List<ThemeDefinition> Load(string filePath)
        {
            var result = new List<ThemeDefinition>();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return result;

            try
            {
                string json = File.ReadAllText(filePath);
                ThemeConfigContainer? container = JsonSerializer.Deserialize<ThemeConfigContainer>(json, Options);
                List<ThemeConfig>? themes = container?.ThemeConfigs?.Themes;
                if (themes == null || themes.Count == 0) return result;

                foreach (ThemeConfig cfg in themes)
                {
                    if (string.IsNullOrWhiteSpace(cfg.Name)) continue;
                    result.Add(new ThemeDefinition
                    {
                        Name = cfg.Name!,
                        DisplayName = string.IsNullOrWhiteSpace(cfg.DisplayName) ? cfg.Name! : cfg.DisplayName!,
                        IsDefault = cfg.IsDefault,
                        Colors = cfg.Colors ?? new Dictionary<string, string>(),
                    });
                }
                return result;
            }
            catch (Exception)
            {
                // json 结构损坏：返回空，让调用方记录日志后走兜底，不阻断应用启动
                return result;
            }
        }
    }
}