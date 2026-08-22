using System.IO;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// 图片查看器数据项（零业务依赖）。
/// 宿主将自身图片模型转换为本类型后传入 <see cref="ImageViewerWindow.ShowImages"/>。
/// </summary>
public class ImageViewItem
{
	/// <summary>图片绝对路径。</summary>
	public string? ImagePath { get; set; }

	/// <summary>文件名（从路径推导，未指定路径时为空）。</summary>
	public string FileName
	{
		get
		{
			string? path = ImagePath;
			return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
		}
	}
}

