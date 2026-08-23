# LundyUI.Controls

![LundyUI Logo](https://raw.githubusercontent.com/fireworkseasycold/LundyUI/main/LundyUI/Resources/Images/LundyUI-Logo.png)

独立 WPF 控件库（纯皮肤可复用框架，业务零依赖）。提供主题引擎、原生控件样式、自定义控件三大能力。

## 版本

| 组件 | 说明 | 版本 |
|------|------|------|
| `LundyUI.Controls` | WPF 控件库（NuGet：`LundyUI.Controls`） | **1.0.4** |
| `LundyUI.Demo` | 主题样板示范程序（与控件库严格对应） | 1.0.4 |

> 版本号统一由仓库根目录 `Directory.Build.props` 的 `<Version>` 维护，UI 库与 Demo **共享同一版本**，升级时只改这一处即可双向同步。

---

## 1. 主题本质："容器 / 骨 / 肉 / 皮" 四层模型

一个**完整主题**需要先厘清概念：在 WPF 里，样式（Style）和模板（ControlTemplate）**本身也是资源**，都以 `x:Key` 或隐式 TargetType 的形式存放在 `ResourceDictionary` 里。所以严谨的表述是——主题是**一个 ResourceDictionary（容器）**，里面装着下面四层：

| 层次 | 构成元素 | 作用 | 比喻 | LundyUI 目录 |
|------|----------|------|------|--------------|
| 容器 | `ResourceDictionary` | 寄存所有主题项的统一载体 | 抽屉/工具箱 | `Generic.xaml`（装配入口） |
| 皮 | `Color` / `Brush` / `FontFamily` / `Thickness` / `CornerRadius` | 视觉原子值，主题的"原料" | 肤色、发色 | `Theming/Resources/` |
| 肉 | `Style` 中的 `Setter` | 决定"什么部件用什么原料" | 骨架的连接关节 | `Styles/*.xaml` |
| 骨 | `ControlTemplate` / `DataTemplate` / `ItemsPanelTemplate` | 决定控件内部视觉树结构（Button 圆角/直角、ScrollBar 样式） | 脸型轮廓 | `Styles/*.xaml`（内嵌）|

> ⚠️ 常见误区：认为"资源 = 颜色/画笔/字体，与样式、模板平级"。实际上样式和模板**也是资源**，只是被 `x:Key` / `TargetType` 索引的内容项。颜色/画笔只是资源里"纯数据"的那一类。

### 1.1 换肤（Skinning）≠ 主题（Theme）

| | 换肤 Skinning | 主题 Theme |
|---|---|---|
| 替换范围 | 仅替换"皮"层（Color/Brush） | 皮 + 肉 + 骨（含模板、动画、间距） |
| 是否改控件结构 | 否 | 是（可整体换一套模板） |
| WPF 实现 | 换资源字典里的色值 | 换资源字典里的 Style/ControlTemplate |

LundyUI 目前的 `ThemeManager` 实现的是**换肤**（多主题切换时只换颜色变量，不改样式和模板）。你的业务已有足够的分工基础，未来如需"换骨头"（Dark 模式用不同模板），只需在 `Styles/` 里提供两套样式、按主题名切换即可，皮/肉/骨分层已就位。

### 1.2 让"皮肉联动"的两个关键字

决定换肤是否跟手的，不是分层本身，而是资源引用关键字：

| 关键字 | 行为 | 换肤表现 |
|--------|------|----------|
| `StaticResource` | 编译期一次性取值，之后固化 | ❌ 换主题不跟手 |
| `DynamicResource` | 运行时随资源字典原位替换自动刷新 | ✅ 换肤能动的根本 |

**因此主题体系硬性约定：所有涉及主题键的颜色/画笔/字体一律用 `DynamicResource`，禁止硬编码色值和 `StaticResource`。**

### 1.3 容器与骨肉皮：从"概念"到"文件"的完整对照

`皮/肉/骨`不是抽象概念，它们都落在具体的 `ResourceDictionary`（容器）里。下表把**容器**与其承载的**层**一一对应：

| 容器（ResourceDictionary） | 文件 | 承载层 | 装的内容 | 何时加载 |
|---|---|---|---|---|
| 装配入口 | `Generic.xaml` | 容器本身 | `MergedDictionaries` 合并下面所有字典（等价原 `_TemplatesMerged.xaml`） | 库加载时一次 |
| 兜底字典 | `Theming/Resources/ThemeShared.xaml` | 皮（+ 基架） | 全部主题键的**浅色默认值**（Color / Brush / FontFamily） | 库加载时**恒久**加载 |
| 活动主题字典 | `Theming/Resources/ActiveTheme.xaml` | 皮 | 空占位，启动后由 `ThemeManager` **原位替换**为当前主题的颜色变量 | 启动时原位替换 |
| 控件样式集 | `Styles/Fonts.xaml`、`ButtonsAndToggles.xaml`、`InputAndSurfaces.xaml`、`Calendar.xaml`、`DataGrid.xaml`、`ListControls.xaml` | 骨 + 肉 | `ControlTemplate`（骨，决定视觉树结构）+ `Style` 的 `Setter`（肉，决定部件用什么原料）；颜色一律 `DynamicResource` 去"皮"里取 | 库加载时一次 |

替换链路——「换肤」只换中间这一环：

```
    皮（通这个色值）          肉（Setter 选部件用什么皮）                  骨（ControlTemplate 定结构）
ThemeShared 兜底 / ActiveTheme  →   Styles/*.xaml 里的 Setter 引用皮键  →   Styles/*.xaml 内嵌的模板
        ↑ 原位替换（ThemeManager 换 ActiveTheme 值）
```

> 谁的"皮"在变：只有 `ActiveTheme` 活动字典在替换颜色/画笔/字体这一"皮"层；`肉`、`骨`不动。所以 LundyUI 当前实现的是**换肤**而非"换主题模板"——若未来要换"骨"（如 Dark 用不同 ControlTemplate），在 `Styles/` 提供两套模板按主题名切换即可，皮/肉/骨分工已就位。

---

## 2. 目录结构

```
LundyUI/
├── LundyUI.Controls.csproj        # 控件库（net6/8/9-windows 多目标，含 NuGet 打包元数据）
├── Generic.xaml                    # 装配入口：合并 皮(ThemeShared) + 肉骨(Styles)
├── Theming/                        # 皮 + 引擎
│   ├── ThemeManager.cs             # 全局单例：多主题切换、原位替换、按键类型解析
│   ├── ThemeDefinition.cs          # 主题数据模型（纯颜色变量表）
│   ├── ThemeConfig.cs              # themes-config.json 数据契约（ThemeConfigs/ThemeConfig）
│   ├── ThemeJsonLoader.cs          # json 驱动加载器：json → ThemeDefinition
│   └── Resources/
│       ├── ThemeShared.xaml        # 主题键兜底（浅色默认值，恒久加载）
│       └── ActiveTheme.xaml        # 活动主题字典占位（ThemeManager 启动时原位替换）
├── Styles/                         # 肉 + 骨：原生控件样式/模板
│   ├── Fonts.xaml                  # 全局字体（AppFontFamily）
│   ├── ButtonsAndToggles.xaml      # Button/ToggleButton/RadioButton/CheckBox
│   ├── InputAndSurfaces.xaml       # TextBox/ComboBox/ProgressBar/Border 等
│   ├── Calendar.xaml               # Calendar/DatePicker
│   ├── DataGrid.xaml               # DataGrid
│   └── ListControls.xaml           # ListBox/ListView/ScrollBar 等
└── CustomControls/                 # 自定义新控件
    ├── DateTimePicker.xaml(.cs)    # 日期时间选择（日历+时分秒+确定回写）
    ├── LoadingCircleControl.xaml(.cs) # 加载圈（圆环描边+无限旋转）
    ├── PaginationControl.xaml(.cs) # 分页（◀ 当前/总数 ▶ + GO 跳转，无页码/省略号）
    ├── ImageViewerWindow.xaml(.cs) # 图片查看器（缩放/拖拽/上一张下一张；多语言可注入，零业务依赖）
    ├── ImageViewItem.cs            # 图片查看器数据项（宿主将自身图片模型转换为本类型传入）
    └── FunctionEventArgs.cs        # 库内泛型事件参数（Pagination 用）
```

> 说明：`Converters/`、`Behaviors/` 为规划中的目录，当前版本未包含。

---

## 3. 如何在一个 WPF 项目中使用 LundyUI

三种引用方式，按场景选择：

| 方式 | 场景 | 说明 |
|------|------|------|
| A · NuGet 包引用 | 版本已发布，下游/生产使用 | `dotnet add package LundyUI.Controls`，安装即用、零外部依赖、可一键升级 |
| B · 项目引用 | 框架与业务同仓、源码可改 | `ProjectReference`，改框架只改一处 |
| C · 拷贝 dll | 明确不再改源码、独立分发 | 需保持 `pack://` 前缀与程序集名一致 |

**推荐**：开发期用 **B**（同仓联调），版本定稿后发布 NuGet 用 **A**（下游消费）。

### 3.1 方式 A：NuGet 包引用（已发布后）

```bash
dotnet add package LundyUI.Controls
```

- 包名 `LundyUI.Controls`，多目标 `net6.0-windows` / `net8.0-windows` / `net9.0-windows`，消费端匹配任一即可。
- 零外部包依赖：不引入任何传递依赖。
- 集成方式与下方 3.4 完全一致（`pack://application:,,,/LundyUI.Controls;component/...` 资源前缀不变）。

### 3.2 方式 B：项目引用（源码可改，同仓联调）

```xml
<ProjectReference Include="..\LundyUI\LundyUI.Controls.csproj" />
```

### 3.3 方式 C：拷贝 dll 引用（仅分发用）

把 `LundyUI.Controls.dll` 拷到你的 `lib/` 目录，再在 csproj 加 dll 引用。注意：若组件内嵌了 `.xaml` 资源，请确保 `pack://application:,,,/LundyUI.Controls;component/...` 的 URI 前缀与程序集名一致。

### 3.4 集成步骤（以"项目引用"为例）

1. **应用入口（App.xaml）合并 LundyUI 资源**——这是唯一必需的装配动作：

```xml
<Application ... StartupUri="MainWindow.xaml">
  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <!-- LundyUI 装配入口：皮(ThemeShared) + 肉骨(Styles) -->
        <ResourceDictionary Source="pack://application:,,,/LundyUI.Controls;component/Generic.xaml" />
        <!-- 活动主题字典占位：ThemeManager 启动时原位替换 -->
        <ResourceDictionary Source="pack://application:,,,/LundyUI.Controls;component/Theming/Resources/ActiveTheme.xaml" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Application.Resources>
</Application>
```

2. **启动时初始化 ThemeManager：json 驱动**（App.xaml.cs）：

```csharp
using System;
using System.IO;
using LundyUI.Controls.Theming;

protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // 可选：注入日志（业务可用 NLog）
    ThemeManager.Instance.Log = msg => Console.WriteLine(msg);

    // 可选：注入持久化（建议接你的 ui-config.json）
    ThemeManager.Instance.SavedThemeName = () => /* 读上次主题名 */;
    ThemeManager.Instance.SaveThemeName = name => /* 写主题名 */;

    // 配置根目录：指向你自己的 Configs/（内含 themes/themes-config.json）——多项目各自指定路径
    ThemeManager.Instance.ConfigBasePath = Path.Combine(AppContext.BaseDirectory, "Configs");

    // 从 themes-config.json 驱动加载主题（配置 N 个主题即 N 种切换；文件缺失则落到 ThemeShared 浅色兜底）
    ThemeManager.Instance.LoadThemesFromConfig();

    // 按"上次保存 / 默认标记 / 第一个"顺序应用
    ThemeManager.Instance.Initialize();
}
```

3. **开发页面**：所有颜色走 `DynamicResource` 主题键：

```xml
<Button Background="{DynamicResource AccentBrush}"
        Foreground="{DynamicResource TextOnAccentBrush}"
        Content="确定" />
```

4. **切换主题**（业务菜单里做主题入口时）：

```csharp
// 填充下拉框
foreach (var t in ThemeManager.Instance.AvailableThemes)
    ThemeBox.Items.Add(new ComboBoxItem { Content = t.DisplayName, Tag = t.Name });

// 切换
ThemeManager.Instance.SwitchTheme("Dark");
```

5. **运行时安全兜底**：窗口加载完成后建议调用一次

```csharp
Loaded += (_, _) => ThemeManager.Instance.ReApplyCurrentTheme();
```

> 详情可参考仓库内 `LundyUI.Demo/` 工程（含 Global 注册、基础控件页、`10. 自定义控件`页）。

---

## 4. 常用主题键速查（来自 ThemeShared.xaml）

| 类别 | 键 |
|------|-----|
| 背景 | `WindowBackBrush` `ContentBackBrush` `PanelBackBrush` `PanelLightBrush` `CardBackBrush` `CardHeaderBackBrush` |
| 边框/分隔 | `BorderNormalBrush` `PanelBorderBrush` `SplitterBrush` |
| 强调/语义 | `AccentBrush` `PrimaryBrush` `DangerBrush` `SuccessBrush` |
| 文本 | `TextDarkBrush` `TextSecondaryBrush` `TextMutedBrush` `TextDisabledBrush` `TextOnAccentBrush` `TextOnDarkBrush` |
| 状态色 | `StatusGreenBrush` `StatusRedBrush` `StatusOrangeBrush` `StatusBlueBrush` `StatusPurpleBrush` `StatusGrayBrush` |
| 菜单/标题栏 | `MenuItemBackBrush` `MenuItemHoverBrush` `MenuItemSelectedBrush` `MenuItemTextBrush` `TitleBarBackBrush` `TitleBarTextBrush` |
| 终端 | `TerminalBackBrush` `TerminalTextBrush` |

**命名约定（ThemeManager 强制）**：
- 以 `Color` 结尾的键 → 生成 `Color` 资源（如 `AccentColor`）。
- 其余键 → 生成 `SolidColorBrush` 资源。
- Brush 属性只能绑 `*Brush` 键，Color 属性只能绑 `*Color` 键。

---

## 5. 自定义控件用法

```xml
xmlns:cc="clr-namespace:LundyUI.Controls.CustomControls;assembly=LundyUI.Controls"
```

### DateTimePicker（日期时间选择）
```xml
<cc:DateTimePicker SelectedDateTime="{Binding StartTime}"
                   DateTimeFormat="yyyy-MM-dd HH:mm:ss" />
```

### LoadingCircle（加载圈）
```xml
<cc:LoadingCircleControl Width="36" Height="36"
                         Foreground="{DynamicResource AccentBrush}" />
```

### Pagination（分页）
```xml
<cc:PaginationControl MaxPageCount="8" PageIndex="2"
                      IsJumpEnabled="True" PageUpdated="OnPageUpdated" />
```

### ImageViewer（图片查看器）
```csharp
using LundyUI.Controls.CustomControls;

var viewer = new ImageViewerWindow();
// 可选：覆盖静态 Localize 接入宿主多语言字典（默认内置中文）
ImageViewerWindow.Localize = key => MyLang.TryGetValue(key, out var t) ? t : key;
// 将自身图片模型转换为 ImageViewerItem 后传入，并指定起始索引与标题
var items = myImages.Select(i => new ImageViewerItem { ImagePath = i.Path }).ToList();
viewer.ShowImages(items, startIndex: 0, title: "视觉数据");
viewer.Show();
```
- 支持缩放（滚轮/加减/适应窗口/原始大小）、按住拖拽、上一张/下一张导航。
- `ImageViewerWindow.Localize` 为静态委托，宿主可覆盖以接入自身多语言；默认内置中文。

```csharp
// PageUpdated 事件携带新页码
private void OnPageUpdated(object? sender, FunctionEventArgs<int> e)
    => LoadPage(e.Info);
```

---

## 6. 约定与约束

- **禁止硬编码色值**：所有颜色引用 `DynamicResource` 主题键。
- **禁止 `StaticResource` 引用主题键**：否则无法随换肤动态更新。
- **业务层不得修改库内样式**：仅允许覆盖资源键（在 App/窗口层提供同名资源覆盖默认值）。
- **基于原生控件优先**：能"微调属性"就不重写模板；需要改结构时才提供 ControlTemplate。
- **尽量少用 `BasedOn`**：优先让一个样式自包含模板/Setter，降低链式继承脆弱性。

---

## 7. Demo

`LundyUI.Demo/` 演示了完整集成：顶部主题切换条 + 复刻的主题样板（ThemePalette，11 节控件平铺 + 点击放大预览浮层），主题由 `Configs/themes/themes-config.json` 驱动（默认 9 套）。完整集成示例（含 json 驱动注册、项目引用）见仓库 `LundyUI.Demo/` 的 `App.xaml.cs` 与 `MainWindow.xaml`。

```bash
dotnet run --project LundyUI.Demo/LundyUI.Demo.csproj
```

切换顶部主题下拉框，观察样板页内所有控件（含自定义控件）颜色实时联动；点击任意展示格可放大预览细节。