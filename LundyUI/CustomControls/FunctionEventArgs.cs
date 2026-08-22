using System;

namespace LundyUI.Controls.CustomControls;

/// <summary>
/// 通用泛型事件参数（库内自持，替代 HandyControl.Data.FunctionEventArgs）。
/// 供 Pagination 等自定义控件上抛带数据的新页码/新值。
/// </summary>
public class FunctionEventArgs<T> : EventArgs
{
	public FunctionEventArgs() { }

	public FunctionEventArgs(T info)
	{
		Info = info;
	}

	public T Info { get; set; } = default!;
}