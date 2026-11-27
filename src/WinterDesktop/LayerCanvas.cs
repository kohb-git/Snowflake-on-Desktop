using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WinterDesktop
{
	internal sealed class LayerCanvas : IDisposable
	{
		private IntPtr dc;

		private IntPtr dib;

		private IntPtr old;

		internal Bitmap Bitmap;

		internal Graphics Graphics;

		internal LayerCanvas(int width, int height)
		{
			dc = Native.CreateCompatibleDC(IntPtr.Zero);
			Native.BITMAPINFO info = new Native.BITMAPINFO
			{
				Size = 40u,
				Width = width,
				Height = -height,
				Planes = 1,
				BitCount = 32
			};
			dib = Native.CreateDIBSection(dc, ref info, 0u, out var bits, IntPtr.Zero, 0u);
			if (dib == IntPtr.Zero)
			{
				Native.DeleteDC(dc);
				dc = IntPtr.Zero;
				throw new Win32Exception();
			}
			old = Native.SelectObject(dc, dib);
			Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, bits);
			Graphics = Graphics.FromImage(Bitmap);
		}

		internal void Present(IntPtr handle, Rectangle bounds, byte alpha = byte.MaxValue)
		{
			Native.POINT pos = new Native.POINT(bounds.X, bounds.Y);
			Native.POINT zero = new Native.POINT(0, 0);
			Native.SIZE size = new Native.SIZE(bounds.Width, bounds.Height);
			Native.BLEND bLEND = default(Native.BLEND);
			bLEND.Alpha = alpha;
			bLEND.Format = 1;
			Native.BLEND blend = bLEND;
			if (!Native.UpdateLayeredWindow(handle, IntPtr.Zero, ref pos, ref size, dc, ref zero, 0u, ref blend, 2u))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		public void Dispose()
		{
			Graphics.Dispose();
			Bitmap.Dispose();
			Native.SelectObject(dc, old);
			Native.DeleteObject(dib);
			Native.DeleteDC(dc);
		}
	}
}
