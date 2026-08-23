using System;
using System.IO;
using System.Windows;
using LundyUI.WPF.Theming;

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
        private const string PersistFileName = "lundyui-theme.txt";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.Instance.Log = msg => Console.WriteLine(msg);
            string persistFile = Path.Combine(AppContext.BaseDirectory, PersistFileName);
            ThemeManager.Instance.SavedThemeName = () =>
                File.Exists(persistFile)
                    ? File.ReadAllText(persistFile).Trim()
                    : null;
            ThemeManager.Instance.SaveThemeName = name =>
                File.WriteAllText(persistFile, name);

            ThemeManager.Instance.ConfigBasePath = Path.Combine(AppContext.BaseDirectory, "Configs");
            ThemeManager.Instance.LoadThemesFromConfig();
            ThemeManager.Instance.Initialize();

        }
    }
}
