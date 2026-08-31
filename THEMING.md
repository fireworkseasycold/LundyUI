# LundyUI 主题统一规范

本规范适用于 LundyUI.WPF 主题体系（`ThemeShared.xaml` 出厂默认 + `themes-config.json` 配置驱动），
LundyUI Demo 与所有消费本包的项目（如 TotalWpfControl）一律遵守。

## 1. 三区一致原则（标题 / 菜单 / 主背景）

上标题栏、左侧菜单、主背景三组背景键必须同色或同明暗阶：

- 默认要求完全同色：`TitleBarBackBrush` = `WindowBackBrush` = `ContentBackBrush` = `MenuItemBackBrush`
- 仅当明确设计"菜单做视觉区隔"时，菜单允许 ±1 档明暗差，且必须同步调整 `MenuItemTextBrush` /
  `MenuItemHoverBrush` / `MenuItemSelectedBrush` 保证可读与协调
- 验收：打开主题样板页，三个区域不得出现明显色块断层（如深色主题中出现白底菜单）

## 2. 键命名约定

- 以 `Color` 结尾的键 → `Color` 资源；其余键 → `SolidColorBrush`
- 背景键一律 `*BackBrush`；文本键一律 `Text*Brush`；边框键 `*BorderBrush`
- 键名必须与 `ThemeShared.xaml` 完全对应；`themes-config.json` 缺键时回落 ThemeShared 默认色（掉色），
  新增主题需保证键集完整

## 3. 明暗原则

- 暗底配亮字、亮底配暗字，禁止明暗混搭（如浅底深字在暗主题、深底浅字在浅主题）
- 选中/悬浮态必须保证对比度：选中背景变亮 → 文字用深色；选中背景变深 → 文字用浅色

## 4. 样板页 = 唯一验收权威

- Demo 工程 `ThemePaletteView` 为权威样板（11 分类：主题色板 / 基础交互 / 输入编辑 / 数据展示 /
  布局导航 / 容器 / 日历 / 状态反馈 / 终端日志 / 自定义控件 / 综合业务样张）
- 消费项目必须整体复用 Demo 同款样板（改为引用本包命名空间），**禁止各项目自造简化版**
- 样板页数据一律硬编码（`DemoMenu` / `Machines` / `EditableRows`），不依赖任何业务 JSON
- 发版验收流程：切换主题 → 打开样板页 → 核对三区一致 + 明暗可读

## 5. 版本与发布流程

- 修改库内样式/模板 → 升 `Directory.Build.props` 的 `<Version>` → commit → push →
  `git tag vX.Y.Z` → `git push origin vX.Y.Z` 触发 `nuget-publish.yml` 自动发布
- 消费项目更新 `PackageReference` 版本号，重新 build 并用样板页验收

## 6. 禁止事项

- 任何 XAML/代码中硬编码颜色值（一律 `DynamicResource` 主题键）
- 消费项目自造简化样板页 / 各写一套规范；与 Demo 效果不一致时以 Demo 为准并回灌回 LundyUI