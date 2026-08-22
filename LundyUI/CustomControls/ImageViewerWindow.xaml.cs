using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// 图片查看器（零业务依赖）。宿主构造后调用 <see cref="ShowImages"/> 并 Show()。
/// 多语言：通过覆盖静态 <see cref="Localize"/> 接入宿主字典；默认内置中文。
/// </summary>
public partial class ImageViewerWindow : Window
{
	private static readonly Dictionary<string, string> _defaults = new()
	{
		["img_viewer_title"] = "图片查看",
		["img_zoom_out"] = "缩小",
		["img_zoom_in"] = "放大",
		["img_fit_window"] = "适应窗口",
		["img_actual_size"] = "实际大小",
		["img_close"] = "关闭",
		["img_prev"] = "上一张",
		["img_next"] = "下一张",
	};

	/// <summary>本地化文本提供器（键 → 文本）。默认返回内置中文；宿主可覆盖接入自身多语言。</summary>
	public static Func<string, string> Localize { get; set; } = key =>
	{
		if (_defaults.TryGetValue(key, out string? text) && !string.IsNullOrEmpty(text))
		{
			return text;
		}
		return key;
	};

	private List<ImageViewItem> _images;
	private int _currentIndex;
	private double _currentScale = 1.0;

	private const double MinScale = 0.1;
	private const double MaxScale = 10.0;

	private bool _isDragging;
	private Point _dragStartPoint;
	private Point _scrollStartOffset;

	public ImageViewerWindow()
	{
		InitializeComponent();
		ApplyLocalization();
		_images = new List<ImageViewItem>();
		_currentIndex = 0;
	}

	private void ApplyLocalization()
	{
		TitleText.Text = Localize("img_viewer_title");
		ZoomOutButton.ToolTip = Localize("img_zoom_out");
		ZoomInButton.ToolTip = Localize("img_zoom_in");
		FitWindowButton.Content = Localize("img_fit_window");
		FitWindowButton.ToolTip = Localize("img_fit_window");
		ActualSizeButton.ToolTip = Localize("img_actual_size");
		CloseButton.ToolTip = Localize("img_close");
		PrevButton.Content = Localize("img_prev");
		PrevButton.ToolTip = Localize("img_prev");
		NextButton.Content = Localize("img_next");
		NextButton.ToolTip = Localize("img_next");
	}

	public void ShowImages(List<ImageViewItem> images, int startIndex, string title)
	{
		if (images == null || images.Count == 0)
		{
			return;
		}
		_images = images;
		_currentIndex = Math.Max(0, Math.Min(startIndex, images.Count - 1));
		TitleDetailText.Text = title;
		UpdateNavigationButtons();
		LoadCurrentImage();
	}

	private void LoadCurrentImage()
	{
		if (_images == null || _images.Count == 0 || _currentIndex < 0 || _currentIndex >= _images.Count)
		{
			return;
		}
		try
		{
			ImageViewItem item = _images[_currentIndex];
			string? path = item.ImagePath;
			if (string.IsNullOrEmpty(path))
			{
				return;
			}
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
			bitmapImage.EndInit();
			MainImage.Source = bitmapImage;
			_currentScale = 1.0;
			UpdateImageScale();
			UpdateUI();
		}
		catch (Exception ex)
		{
			MessageBox.Show("加载图片失败: " + ex.Message, Localize("img_viewer_title"), MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void UpdateUI()
	{
		if (_images != null && _images.Count != 0)
		{
			ImageInfoTextBlock.Text = $"{_currentIndex + 1} / {_images.Count}";
			FileNameTextBlock.Text = _images[_currentIndex].FileName;
			ZoomTextBlock.Text = $"{_currentScale:P0}";
		}
	}

	private void UpdateNavigationButtons()
	{
		if (_images == null)
		{
			return;
		}
		PrevButton.IsEnabled = _currentIndex > 0;
		NextButton.IsEnabled = _currentIndex < _images.Count - 1;
	}

	private void UpdateImageScale()
	{
		if (MainImage.Source != null)
		{
			MainImage.LayoutTransform = new ScaleTransform(_currentScale, _currentScale);
		}
	}

	private void PrevButton_Click(object sender, RoutedEventArgs e)
	{
		if (_currentIndex > 0)
		{
			_currentIndex--;
			LoadCurrentImage();
			UpdateNavigationButtons();
		}
	}

	private void NextButton_Click(object sender, RoutedEventArgs e)
	{
		if (_currentIndex < _images.Count - 1)
		{
			_currentIndex++;
			LoadCurrentImage();
			UpdateNavigationButtons();
		}
	}

	private void ZoomInButton_Click(object sender, RoutedEventArgs e) => ZoomImage(1.2);

	private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => ZoomImage(0.8);

	private void FitToWindowButton_Click(object sender, RoutedEventArgs e)
	{
		if (MainImage.Source != null && MainImage.Source is BitmapImage bitmapImage)
		{
			double width = ImageScrollViewer.ActualWidth - 40.0;
			double height = ImageScrollViewer.ActualHeight - 40.0;
			double scaleX = width / bitmapImage.PixelWidth;
			double scaleY = height / bitmapImage.PixelHeight;
			_currentScale = Math.Max(MinScale, Math.Min(Math.Min(scaleX, scaleY), MaxScale));
			UpdateImageScale();
			UpdateUI();
		}
	}

	private void ActualSizeButton_Click(object sender, RoutedEventArgs e)
	{
		_currentScale = 1.0;
		UpdateImageScale();
		UpdateUI();
	}

	private void ZoomImage(double factor)
	{
		double value = _currentScale * factor;
		value = Math.Max(MinScale, Math.Min(value, MaxScale));
		if (Math.Abs(value - _currentScale) > 0.001)
		{
			_currentScale = value;
			UpdateImageScale();
			UpdateUI();
		}
	}

	private void ImageScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
		ZoomImage(e.Delta > 0 ? 1.2 : 0.8);
	}

	private void ImageScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (_currentScale > 1.0)
		{
			_isDragging = true;
			_dragStartPoint = e.GetPosition(ImageScrollViewer);
			_scrollStartOffset = new Point(ImageScrollViewer.HorizontalOffset, ImageScrollViewer.VerticalOffset);
			ImageScrollViewer.Cursor = Cursors.SizeAll;
			ImageScrollViewer.CaptureMouse();
		}
	}

	private void ImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
	{
		if (_isDragging)
		{
			Point position = e.GetPosition(ImageScrollViewer);
			Point delta = new Point(_dragStartPoint.X - position.X, _dragStartPoint.Y - position.Y);
			ImageScrollViewer.ScrollToHorizontalOffset(_scrollStartOffset.X + delta.X);
			ImageScrollViewer.ScrollToVerticalOffset(_scrollStartOffset.Y + delta.Y);
		}
	}

	private void ImageScrollViewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isDragging)
		{
			_isDragging = false;
			ImageScrollViewer.Cursor = Cursors.Arrow;
			ImageScrollViewer.ReleaseMouseCapture();
		}
	}

	private void Window_KeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.Escape:
				Close();
				e.Handled = true;
				break;
			case Key.Prior:
			case Key.Left:
			case Key.Up:
				PrevButton_Click(sender, e);
				e.Handled = true;
				break;
			case Key.Next:
			case Key.Right:
			case Key.Down:
				NextButton_Click(sender, e);
				e.Handled = true;
				break;
			case Key.Home:
				if (_images != null && _images.Count > 0)
				{
					_currentIndex = 0;
					LoadCurrentImage();
					UpdateNavigationButtons();
				}
				e.Handled = true;
				break;
			case Key.End:
				if (_images != null && _images.Count > 0)
				{
					_currentIndex = _images.Count - 1;
					LoadCurrentImage();
					UpdateNavigationButtons();
				}
				e.Handled = true;
				break;
			case Key.J:
			case Key.D1:
				ActualSizeButton_Click(sender, e);
				e.Handled = true;
				break;
			case Key.Add:
			case Key.OemPlus:
				ZoomInButton_Click(sender, e);
				e.Handled = true;
				break;
			case Key.Subtract:
			case Key.OemMinus:
				ZoomOutButton_Click(sender, e);
				e.Handled = true;
				break;
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
