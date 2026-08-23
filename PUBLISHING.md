# LundyUI NuGet 发布流程（Trusted Publishing · 免密钥）

本文档记录 `LundyUI.WPF` 从本地打包到 CI/CD 自动发布到 nuget.org 的完整流程与配置说明。

> 安全说明：本文不包含任何密码、API Key 或账号信息，涉及账号处一律使用占位符 `<占位>`。

---

## 1. 发布方式总览

采用 nuget.org 的 **Trusted Publishing（受信发布）** 机制：

- 通过 OpenID Connect (OIDC)，让 GitHub Actions 向 nuget.org 证明自己是某仓库的合法构建，**无需长期 API Key**。
- 每次构建时由 GitHub 签发一次性的临时 API Key（有效期约 1 小时），用完即失效，不会泄露。

```
┌─────────────┐     OIDC Token      ┌─────────────┐         ┌──────────────┐
│   GitHub     │ ──────────────────→ │  nuget.org  │ ──push──→ │  package    │
│   Actions    │     (无密钥)        │  验证身份   │          │  已上架     │
└─────────────┘                     └─────────────┘         └──────────────┘
```

---

## 2. 目录结构（相关文件）

```
LundyUI/
├── .github/
│   └── workflows/
│       └── nuget-publish.yml      # CI/CD 自动发布工作流
├── Directory.Build.props           # 版本号 / 通用元数据统一维护
├── LundyUI/ (LundyUI.WPF.csproj)
│   ├── Resources/Images/           # Logo / 图标资源（内嵌 + 打包）
│   └── README.md                   # 包的 README
└── LundyUI.Demo/                   # 示范工程（与库共享版本号）
```

---

## 3. 版本号统一管理

根目录 `Directory.Build.props` 集中维护版本号，**UI 库与 Demo 同版本**：

```xml
<Project>
  <PropertyGroup>
    <Version>1.0.1</Version>
    <Authors>LundyUI</Authors>
    <Company>LundyUI</Company>
    <Product>LundyUI</Product>
  </PropertyGroup>
</Project>
```

> 每次发版只需改这一处 `<Version>`，库与 Demo 会自动同步；标签号需与之一致（见第 8 节）。

---

## 4. 库工程打包配置（LundyUI.WPF.csproj）

关键 NuGet 元数据（多目标、许可证、图标、README、符号包）：

```xml
<PropertyGroup>
  <!-- 多目标：覆盖 net6/8/9 消费端 -->
  <TargetFrameworks>net6.0-windows7.0;net8.0-windows7.0;net9.0-windows7.0</TargetFrameworks>
  <UseWPF>true</UseWPF>

  <PackageId>LundyUI.WPF</PackageId>
  <Authors>LundyUI</Authors>
  <Description>独立 WPF 控件库：配置驱动主题引擎（json 换肤）+ 原生控件样式（DynamicResource）+ 自定义控件。</Description>
  <PackageTags>wpf;theme;theming;ui;controls;styles;skinning</PackageTags>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>   <!-- nuget.org 强制要求许可证 -->
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageIcon>L-EXE.png</PackageIcon>
  <NeutralLanguage>zh-CN</NeutralLanguage>

  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <RepositoryUrl>https://github.com/<占位>/LundyUI</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
</PropertyGroup>

<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="\" Visible="false" />

  <!-- 图标 / Logo：既作为内嵌 Resource，也随包发布 -->
  <Resource Include="Resources\Images\LundyUI-Logo.png" />
  <Resource Include="Resources\Images\L-EXE.png" />
  <None Include="Resources\Images\L-EXE.png" Pack="true" PackagePath="\" Visible="false" />
  <None Include="Resources\Images\LundyUI-Logo.png" Pack="true" PackagePath="\" Visible="false" />
</ItemGroup>
```

> 注意：`PackageLicenseExpression` 必须存在，否则 nuget.org 会拒绝接收包。

DEMO 工程的 exe 图标通过 `L-EXE.ico` 设置 `ApplicationIcon`，版本号同样继承自 `Directory.Build.props`。

---

## 5. GitHub Actions 工作流（.github/workflows/nuget-publish.yml）

### 5.1 触发方式

```yaml
on:
  workflow_dispatch:   # 手动触发：Actions → Publish NuGet → Run workflow
  push:
    tags:
      - 'v*'           # 推送 v1.0.1 之类标签自动触发
```

### 5.2 权限

```yaml
permissions:
  contents: read
  id-token: write      # 关键：允许签发 OIDC 令牌（受信发布依赖）
```

### 5.3 步骤要点

```yaml
jobs:
  build-and-publish:
    runs-on: windows-latest          # WPF/XAML 必须用 Windows 运行器
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'    # 覆盖最高 TFM net9

      - run: dotnet restore LundyUI/LundyUI.WPF.csproj

      - run: dotnet pack LundyUI/LundyUI.WPF.csproj -c Release -o ./nupkg

      # Trusted Publishing：OIDC → nuget.org 临时 API Key
      - name: NuGet login (OIDC → temp API key)
        id: login
        uses: NuGet/login@v1
        with:
          user: ${{ secrets.NUGET_USER }}

      # 关键：push 步骤必须用 bash 外壳 + 单行命令
      - name: Push to nuget.org
        shell: bash
        run: dotnet nuget push ./nupkg/*.nupkg --api-key "${{ steps.login.outputs.NUGET_API_KEY }}" --source https://api.nuget.org/v3/index.json --skip-duplicate
```

### 5.4 两个踩坑点（务必遵守）

| 问题 | 现象 | 修复 |
|------|------|------|
| WPF 工程在 Linux 运行器上构建 | 无法生成 XAML / 打包失败 | `runs-on` 改用 `windows-latest` |
| Push 步骤用了 `\` 续行 | PowerShell 不识别 `\`，命令被拆断、`--api-key` 等被当成独立命令而报错 | 给 push 步骤加 `shell: bash`，并写成**单行**命令 |

---

## 6. nuget.org 侧配置（网页操作，仅首次）

1. 登录 nuget.org → 右上角账户头像 → **API Keys**。
2. 进入 **Trusted Publishing（新建）**。
   - Package Owner：`<你的 nuget 用户名>`
   - CI/CD Provider：`GitHub Actions`
   - Repository Owner：`<你的 GitHub 用户名>`
   - Repository：`LundyUI`
   - Workflow File：`nuget-publish.yml`（必须与第 5 节的实体文件名一致）
   - Glob Patterns / Packages：`LundyUI.WPF`（允许发布的具体包名）
3. 保存策略。

完成后，该仓库在该工作流下通过 OIDC 即拥有发布该包名的权限，全程无需长期 API Key。

---

## 7. GitHub 仓库侧配置（Secret，仅首次）

在 GitHub 仓库 `Settings → Secrets and variables → Actions` 中新增一个仓库密钥：

| 密钥名 | 含义 | 值 |
|--------|------|-------|
| `NUGET_USER` | nuget.org 的**用户名**（Profile 名，非邮箱） | `<你的 nuget 用户名>` |

> `NUGET_USER` 供上面第 6 节 Trusted Publishing 对应账户在 OIDC 登录时标识身份使用。不要填邮箱。

---

## 8. 触发生成发布

日常发新版本只需两步：

```bash
# 1. 先修改目录根 Directory.Build.props 的 <Version>（如 1.1.0）
# 2. 打标签并推送，触发 CI
git tag v1.1.0
git push origin v1.1.0
```

GitHub Actions 会自动：restore → pack → OIDC 登录 → push 到 nuget.org。

> 标签号需与 `<Version>` 保持一致；若重复打同名标签，需先删除旧标签再重建（否则会沿用旧提交中的工作流）。

---

## 9. 验证与常见问题

- **验证发布**：工作流 Run 页面显示 **Success**；再通过版本索引确认：
  `https://api.nuget.org/v3-flatcontainer/lundyui.controls/index.json` 应能看到对应版本号。
- **包页面短暂 404**：nuget.org 图库索引有延迟，属正常，稍等即可看到：
  `https://www.nuget.org/packages/LundyUI.WPF`
- **push 报错但 login 成功**：多为第 5.4 节的坑（Linux 运行器 / `\` 续行），按表格修复即可。
- **没有 `PackageLicenseExpression`**：nuget.org 会拒绝接收，务必在 csproj 中声明。

---

## 10. 隐患与可选项

- 发布后可在 README 加 NuGet 徽章：`https://img.shields.io/nuget/v/LundyUI.WPF`
- 建议配合语义化版本与仓库 Release/Tag 规范使用。
- CI 中存在 Node.js 20 弃用警告（actions/checkout@v4、actions/setup-dotnet@v4），后续可将 action 版本升级到基于 Node 24 的版本以消除警告。

---

## 11. Demo 引用方式分析（工程引用 vs NuGet）

Demo 工程同时保留了两种引用方式，但**默认只启用工程引用**：

```xml
<ItemGroup>
  <!-- 默认启用：工程引用（源码开发模式）；验证发布包时再启用下方 NuGet 引用。
       ⚠ 两者不要同时启用，否则同一程序集来自两个来源会报“重复引用/程序集冲突”。 -->
  <ProjectReference Include="..\LundyUI\LundyUI.WPF.csproj" />
  <!-- <PackageReference Include="LundyUI.WPF" Version="1.0.1" /> -->
</ItemGroup>
```

### 两种方式对比

| 维度 | 工程引用 ProjectReference | NuGet 引用 PackageReference |
|------|------|------|
| 开发调试 | 改动即生效，可断点进库源码 | 需重新打包 + 提升版本才能更新 |
| 版本同步 | 与库共享 Directory.Build.props，天然同版本 | 需手动维护引用版本号 |
| 是否验证发布包 | 否，不经过 NuGet 链路 | 是，能真实验证“安装即用” |
| 对源的依赖 | 依赖仓库内源码工程 | 依赖包源可达、版本存在 |

### 判断（结论）

1. **日常开发/调试：应保持工程引用。** Demo 是 UI 库的样张与验证工程，与库同仓库、同版本、同步迭代，工程引用最能提效，符合“直接引用源码、版本由 Directory.Build.props 统一”的协作方式。
2. **仅在特定场景临时切 NuGet 引用**：例如要校准“发布包装到空项目即可用”、给外部使用者做演示、或验证刚发布的版本号在源上生效。验证完应切回工程引用。
3. **不要同时启用两者**。

### 实测验证记录

- 工程引用（状态 A）：`dotnet build -c Release` → 构建成功，0 错误。
- 切换 NuGet 引用（状态 B）：`dotnet build -c Release` → 从 nuget.org 还原并构建成功，0 错误；本地 NuGet 缓存确认存在 `lundyui.controls/1.0.1`（net6/8/9 三个程序集 + 图标 + README）。
- 已切回状态 A（工程引用）作为仓库默认值。
