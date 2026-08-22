using System;
using System.IO;
using System.Windows;
using LundyUI.Controls.Theming;

namespace LundyUI.Demo
{
    /// <summary>
    /// Demo 启动：json 驱动加载主题 + 注入持久化 + 初始化。
    /// 主题来源完全由 Configs/themes/themes-config.json 控制（配置 N 个主题即 N 种切换）。
    /// 配置根目录通过 ThemeManager.ConfigBasePath 注入——不同 WPF 项目各自指向自己的配置目录，
    /// 这正是 LundyUI 被任意项目复用时"只改路径、不动代码"的关键。
    /// </summary>
    public partial class App : Application
    {
        private const string PersistFile = "lundyui-theme.txt";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.Instance.Log = msg => Console.WriteLine(msg);
            // 持久化：读写一个临时文件演示业务如何接入（真实业务会写自己的 ui-config/json）
            ThemeManager.Instance.SavedThemeName = () =>
                PersistFile != null && File.Exists(PersistFile)
                    ? File.ReadAllText(PersistFile).Trim()
                    : null;
            ThemeManager.Instance.SaveThemeName = name =>
                File.WriteAllText(PersistFile, name);

            // 配置根目录：Demo 自带的 Configs/（themes/menu 两个 json 在此）。业务项目只需改此路径。
            ThemeManager.Instance.ConfigBasePath = Path.Combine(AppContext.BaseDirectory, "Configs");

            // 从 themes-config.json 驱动加载主题；文件缺失/失败时为空集，落到 ThemeShared 默认浅色兜底
            ThemeManager.Instance.LoadThemesFromConfig();

            ThemeManager.Instance.Initialize();
        }
    }
}