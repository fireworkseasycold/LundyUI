using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LundyUI.WPF.Converters
{
    /// <summary>图片路径字符串 -> ImageSource；空 / 加载失败返回 null（UI 框架级，通用，不静默抛错）。</summary>
    public sealed class PathToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            catch (Exception ex)
            {
                // 不掩盖配置错误：路径无法作为图片解码时 Debug 告警，便于第一时间定位错误源（如把中文字符当图片路径）。
                if (ex is FileNotFoundException or IOException or NotSupportedException or UriFormatException or FormatException)
                    Debug.WriteLine($"[PathToImageConverter] 无法加载图片路径 '{path}'：{ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
