using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace LundyUI.Controls.Converters
{
    /// <summary>
    /// 图标名 -> MDI 字形 通用转换器（UI 框架级，不含任何业务语义）。
    /// 数据源：随程序集内嵌的 materialdesignicons.codepoints 表（官方 CSS 提取，name <-> codepoint），
    ///         首次使用时懒加载并缓存，覆盖 7400+ 图标，业务/Demo 只需书写图标名。
    /// 约定：未命中时严格解析——Debug 告警并返回空串，不再静默透传 emoji / 中文 / 直接字形，避免"能加载但渲染成方块"。
    /// 使用：Text="{Binding IconName, Converter={StaticResource IconGlyph}}"
    ///       FontFamily="{DynamicResource IconFontFamily}"
    /// </summary>
    public sealed class IconGlyphConverter : IValueConverter
    {
        private const string ResourceName =
            "LundyUI.Controls.Resources.Icons.materialdesignicons.codepoints";

        // 线程安全的懒加载：加载失败退化空表 -> 转换器严格告警并返回空，不抛不阻塞。
        private static readonly Lazy<Dictionary<string, string>> Glyphs =
            new Lazy<Dictionary<string, string>>(LoadGlyphs, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static Dictionary<string, string> LoadGlyphs()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var stream = typeof(IconGlyphConverter).Assembly
                    .GetManifestResourceStream(ResourceName);
                if (stream == null) return map;
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var tab = line.IndexOf('\t');
                    if (tab <= 0) continue;
                    var name = line.Substring(0, tab).Trim();
                    if (name.Length == 0) continue;
                    var hex = line.Substring(tab + 1).Trim();
                    if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
                        map[name] = char.ConvertFromUtf32(cp);
                }
            }
            catch
            {
                // 数据表缺失/解析失败：返回空表，转换器严格告警并返回空，控件仍可加载。
            }
            return map;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string n) return value;
            if (Glyphs.Value.TryGetValue(n, out var glyph)) return glyph;
            // 严格解析：未命中图标名 -> Debug 告警 + 返回空串（移除旧态透传，避免静默方块/原文，便于排查配置拼错）。
            Debug.WriteLine($"[IconGlyphConverter] 未知图标名 '{n}'，已置空。请核对 MDI 图标名，或改走 ImagePath(图片)/文本。");
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}