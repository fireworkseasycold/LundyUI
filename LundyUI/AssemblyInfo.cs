// ============================================================
// LundyUI.WPF 命名空间注册
// 通过 XmlnsDefinition 将 URL 映射到多个 CLR 命名空间，消费端只需写
//   xmlns:lui="https://github.com/fireworkseasycold/LundyUI"
// 即可直接使用 lui:DateTimePicker / lui:LoadingCircleControl / lui:PaginationControl
// / lui:ThemeManager 等所有公开控件。这是 HandyControl / MahApps 等主流 WPF UI 库的标准统一入口。
// ============================================================
using System.Windows.Markup;

[assembly: XmlnsDefinition("https://github.com/fireworkseasycold/LundyUI", "LundyUI.WPF")]
[assembly: XmlnsDefinition("https://github.com/fireworkseasycold/LundyUI", "LundyUI.WPF.CustomControls")]
[assembly: XmlnsDefinition("https://github.com/fireworkseasycold/LundyUI", "LundyUI.WPF.Theming")]
[assembly: XmlnsDefinition("https://github.com/fireworkseasycold/LundyUI", "LundyUI.WPF.Converters")]
[assembly: XmlnsPrefix("https://github.com/fireworkseasycold/LundyUI", "lui")]
