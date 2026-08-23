using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LundyUI.Controls.Theming
{
    /// <summary>
    /// 全局主题管理器（单例）：配置驱动的多主题切换。
    /// 机制：主题定义为纯颜色变量（ThemeDefinition.Colors），切换时用配置的颜色在运行时
    /// 构建 ResourceDictionary，原位替换 App.Resources.MergedDictionaries 中带内部标记键
    /// __ThemeDictionary__ 的活动主题字典；所有界面颜色通过 DynamicResource 引用主题键，
    /// 替换后自动全局生效。
    /// 本类零业务依赖：主题来源（AddThemes）、持久化（SavedThemeName/SaveThemeName）、
    /// 日志（Log）均由业务通过属性注入；出厂兜底 = 空覆盖（落在 ThemeShared 浅色默认）。
    /// </summary>
    public sealed class ThemeManager
    {
        /// <summary>活动主题字典内部标记键：用于在 MergedDictionaries 中识别并原位替换活动主题字典</summary>
        public const string ThemeDictionaryMarker = "__ThemeDictionary__";

        private static readonly Lazy<ThemeManager> _instance = new(() => new ThemeManager());
        private readonly List<ThemeDefinition> _themes = new();

        private ThemeManager() { }

        /// <summary>配置根目录（业务启动时注入，如指向各自 Configs/ 目录）。json 加载依赖此属性定位 themes-config.json。</summary>
        public string ConfigBasePath { get; set; } = string.Empty;

        /// <summary>主题来源（业务在启动时注册，如从 themes-config.json 读取）</summary>
        public void AddThemes(IEnumerable<ThemeDefinition> themes)
        {
            if (themes == null) return;
            foreach (ThemeDefinition t in themes)
            {
                if (string.IsNullOrWhiteSpace(t.Name)) continue;
                _themes.RemoveAll(x => x.Name == t.Name);
                _themes.Add(t);
            }
        }

        /// <summary>
        /// 从配置加载主题（json 驱动）：定位 <see cref="ConfigBasePath"/> 下的 themes-config.json，
        /// 解析 ThemeConfigs.Themes 后注册到主题列表。文件缺失/解析失败返回 false（不抛异常）。
        /// 与 AddThemes 可混用：本方法幂等（同名主题覆盖）。
        /// </summary>
        public bool LoadThemesFromConfig()
        {
            if (string.IsNullOrWhiteSpace(ConfigBasePath)) return false;
            string file = Path.Combine(ConfigBasePath, "themes", "themes-config.json");
            if (!File.Exists(file))
            {
                Log?.Invoke($"[LundyUI.Theme] 未找到主题配置: {file}");
                return false;
            }
            List<ThemeDefinition> themes = ThemeJsonLoader.Load(file);
            AddThemes(themes);
            Log?.Invoke($"[LundyUI.Theme] 已从 {file} 加载 {themes.Count} 个主题");
            return themes.Count > 0;
        }

        /// <summary>单例实例（调用前应先经业务 AddThemes + 注入持久化，再调用 Initialize）</summary>
        public static ThemeManager Instance => _instance.Value;

        /// <summary>可用主题列表（业务注册的顺序即切换下拉框显示顺序）</summary>
        public IReadOnlyList<ThemeDefinition> AvailableThemes { get { lock (_themes) return _themes.ToList(); } }

        /// <summary>当前主题名（未应用时为空）</summary>
        public string CurrentTheme { get; private set; } = string.Empty;

        /// <summary>主题切换完成事件（UI 可据此同步切换入口状态）</summary>
        public event EventHandler? ThemeChanged;

        /// <summary>日志回调（业务注入，如接 NLog）；为空则静默</summary>
        public Action<string>? Log { get; set; }

        /// <summary>持久化读取：返回上次保存的主题名；为空则按默认/兜底顺序</summary>
        public Func<string?>? SavedThemeName { get; set; }

        /// <summary>持久化写入：主题切换成功后回调业务保存（如写 ui-config.json）</summary>
        public Action<string>? SaveThemeName { get; set; }

        /// <summary>切换主题到指定名称；未知主题名忽略并返回 false</summary>
        public bool SwitchTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName)) return false;
            if (!_themes.Any(t => t.Name == themeName))
            {
                Log?.Invoke($"[LundyUI.Theme] 未知主题名: {themeName}，忽略切换");
                return false;
            }
            ApplyInPlace(themeName);
            SaveThemeName?.Invoke(themeName);
            return true;
        }

        /// <summary>
        /// 启动初始化：按"上次保存的主题 / 默认标记主题 / 第一个主题 / 兜底空覆盖"顺序应用。
        /// 应在业务 AddThemes + 注入持久化后调用一次。
        /// </summary>
        public void Initialize()
        {
            string? saved = SavedThemeName?.Invoke();
            ThemeDefinition? target = _themes.FirstOrDefault(t => t.Name == saved)
                ?? _themes.FirstOrDefault(t => t.IsDefault)
                ?? _themes.FirstOrDefault();
            if (target != null)
            {
                ApplyInPlace(target.Name);
                return;
            }
            // 业务未注册任何主题：应用兜底空字典（无覆盖，落在 ThemeShared 浅色默认）
            ApplyInPlace(null);
        }

        /// <summary>重新应用当前主题（窗口加载完成后的安全兜底，确保启动时错过的改动在 UI 就绪后再次生效）</summary>
        public void ReApplyCurrentTheme()
            => ApplyInPlace(string.IsNullOrEmpty(CurrentTheme) ? null : CurrentTheme);

        /// <summary>
        /// 应用主题：在 App.Resources.MergedDictionaries 中定位活动主题字典（按 Source 名含
        /// ActiveTheme 或含标记键）并原位替换。原位替换（而非 Clear+Add）保证相邻字典的顺序与引用不受影响。
        /// </summary>
        private void ApplyInPlace(string? themeName)
        {
            try
            {
                if (Application.Current == null) return;

                ThemeDefinition? theme = themeName == null
                    ? null
                    : _themes.FirstOrDefault(t => t.Name == themeName);

                var dictionaries = Application.Current.Resources.MergedDictionaries;
                int index = dictionaries.ToList().FindIndex(d =>
                    (d.Source != null && d.Source.OriginalString.IndexOf("ActiveTheme", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    d.Contains(ThemeDictionaryMarker));

                ResourceDictionary dictionary = BuildDictionary(theme);
                if (index < 0)
                {
                    // 活动主题字典缺失（异常部署）：追加兜底，保证 DynamicResource 有键可解析
                    dictionaries.Add(dictionary);
                }
                else
                {
                    dictionaries[index] = dictionary;
                }

                CurrentTheme = theme?.Name ?? string.Empty;
                ThemeChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[LundyUI.Theme] 应用主题 {themeName} 失败: {ex.Message}");
            }
        }

        /// <summary>构建活动主题字典：遍历颜色变量表构建；theme 为空时返回仅含标记的兜底字典</summary>
        private ResourceDictionary BuildDictionary(ThemeDefinition? theme)
        {
            var dictionary = new ResourceDictionary();
            // 写入内部标记，便于后续原位替换时识别运行时构建的主题字典
            dictionary[ThemeDictionaryMarker] = theme?.Name ?? "fallback";
            if (theme?.Colors == null) return dictionary;

            foreach (KeyValuePair<string, string> kv in theme.Colors)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(kv.Value);
                    if (kv.Key.EndsWith("Color", StringComparison.OrdinalIgnoreCase))
                    {
                        dictionary[kv.Key] = color;
                    }
                    else
                    {
                        var brush = new SolidColorBrush(color);
                        brush.Freeze(); // Freeze 后不可变：可跨线程共享，并降低 WPF 动画/绑定 GC 压力
                        dictionary[kv.Key] = brush;
                    }
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[LundyUI.Theme] 颜色值解析失败: {theme.Name}.{kv.Key} = {kv.Value}: {ex.Message}");
                }
            }

            List<string> missing = GetThemeSharedKeys().Except(dictionary.Keys.Cast<object>().Select(k => k?.ToString() ?? string.Empty)).ToList();
            if (missing.Count > 0)
            {
                Log?.Invoke($"[LundyUI.Theme] 主题 {theme.Name} 缺少 {missing.Count} 个键: {string.Join(", ", missing)}（将回落到 ThemeShared 默认色）");
            }
            return dictionary;
        }

        /// <summary>ThemeShared 兜底字典的键集合（作为主题键的权威清单）</summary>
        private static List<string> GetThemeSharedKeys()
        {
            try
            {
                var dictionaries = Application.Current.Resources.MergedDictionaries;
                ResourceDictionary? shared = dictionaries.FirstOrDefault(d =>
                    d.Source != null && d.Source.OriginalString.IndexOf("ThemeShared", StringComparison.OrdinalIgnoreCase) >= 0);
                return shared?.Keys.Cast<object>().Select(k => k?.ToString() ?? string.Empty).ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
