using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LundyUI.Controls.Converters
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
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
