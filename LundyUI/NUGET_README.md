# LundyUI.WPF

![LundyUI Logo](https://raw.githubusercontent.com/fireworkseasycold/LundyUI/main/LundyUI/Resources/Images/LundyUI-Logo.png)

独立 WPF 控件库（纯皮肤可复用框架，业务零依赖）：配置驱动主题引擎（json 换肤）、原生控件样式（DynamicResource）、自定义控件三大能力。任何 WPF 项目引用即可获得完整主题能力。

## 安装

```bash
dotnet add package LundyUI.WPF
```

支持 `net6.0-windows` / `net8.0-windows` / `net9.0-windows`，零外部依赖。

## 快速使用

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="pack://application:,,,/LundyUI.WPF;component/Generic.xaml" />
      <ResourceDictionary Source="pack://application:,,,/LundyUI.WPF;component/Theming/Resources/ActiveTheme.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

```csharp
ThemeManager.Instance.ConfigBasePath = Path.Combine(AppContext.BaseDirectory, "Configs");
ThemeManager.Instance.LoadThemesFromConfig();
ThemeManager.Instance.Initialize();
ThemeManager.Instance.SwitchTheme("Dark");
```

页面内所有颜色一律使用 `DynamicResource` 主题键（如 `{DynamicResource AccentBrush}`）。

## 内置自定义控件

- **DateTimePicker**：日期时间选择（日历 + 时分秒 + 确定回写）
- **LoadingCircleControl**：加载圈
- **PaginationControl**：分页（前 当前/总数 后 + GO 跳转）
- **ImageViewerWindow**：图片查看器（缩放/拖拽/上一张下一张，多语言可注入）

## 版本

| 组件 | 版本 |
|------|------|
| `$11.1.0 |
| `LundyUI.Demo`（示范程序） | 1.0.1 |

完整集成步骤、主题键速查、Demo 说明见仓库 `LundyUI/README.md`。