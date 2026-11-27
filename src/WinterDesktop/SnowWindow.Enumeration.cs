using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WinterDesktop
{
	public sealed partial class SnowWindow
	{
		internal static bool ValidFrameInsets(Rectangle insets)
		{
			if (insets.X >= 0 && insets.Y >= 0 && insets.Width >= 0 && insets.Height >= 0 && insets.X <= 32 && insets.Y <= 32 && insets.Width <= 32)
			{
				return insets.Height <= 32;
			}
			return false;
		}

		public static Rectangle FollowNativeFrame(Rectangle raw, Rectangle insets)
		{
			return Rectangle.FromLTRB(raw.Left + insets.Left, raw.Top + insets.Top, raw.Right - insets.Width, raw.Bottom - insets.Height);
		}

		internal static bool IsTransparentSnapHost(string className, int exStyle)
		{
			if (className == "XamlExplorerHostIslandWindow")
			{
				return (exStyle & 0x800A0) == 524448;
			}
			return false;
		}

		internal static bool CanDecorateWindow(string className, int style, int exStyle, Rectangle bounds)
		{
			if (bounds.Width < 100 || bounds.Height < 60)
			{
				return false;
			}
			if (((uint)exStyle & 0x8000080u) != 0)
			{
				return false;
			}
			switch (className)
			{
			case "#32768":
			case "tooltips_class32":
			case "XamlExplorerHostIslandWindow":
			case "Microsoft.UI.Content.PopupWindowSiteBridge":
			case "Windows.UI.Input.InputSite.WindowClass":
			case "SysShadow":
			case "MozillaDropShadowWindowClass":
				return false;
			default:
				if (((uint)style & 0x80000000u) != 0 && (style & 0xC40000) == 0 && (exStyle & 0x40000) == 0)
				{
					return false;
				}
				return true;
			}
		}

		internal static bool IsInputBackdrop(string cls, string processName, int exStyle)
		{
			if (cls == "Windows.UI.Core.CoreWindow" && string.Equals(processName, "TextInputHost", StringComparison.OrdinalIgnoreCase))
			{
				return (exStyle & 0x200000) != 0;
			}
			return false;
		}

		public void RefreshWindows()
		{
			IntPtr foreground = Native.GetForegroundWindow();
			List<WindowInfo> candidates = new List<WindowInfo>();
			HashSet<IntPtr> live = new HashSet<IntPtr>();
			List<Rectangle> taskbars = new List<Rectangle>();
			Native.EnumWindows(delegate(IntPtr h, IntPtr p)
			{
				Native.GetWindowThreadProcessId(h, out var process);
				if ((process == processId && (h == base.Handle || (garlandWindow.IsHandleCreated && h == garlandWindow.Handle))) || !Native.IsWindowVisible(h) || Native.IsIconic(h))
				{
					return true;
				}
				if (Native.DwmGetWindowAttribute(h, 14, out int value, 4) == 0 && value != 0)
				{
					return true;
				}
				StringBuilder stringBuilder = new StringBuilder(128);
				Native.GetClassName(h, stringBuilder, 128);
				string text = stringBuilder.ToString();
				if (text == "Progman" || text == "WorkerW")
				{
					return true;
				}
				int windowLong = Native.GetWindowLong(h, -20);
				bool invisibleLayer = (windowLong & 0x80000) != 0 &&
					Native.GetLayeredWindowAttributes(h, out _, out byte alpha, out uint flags) &&
					(flags & 2) != 0 && alpha == 0;
				const int clickThroughToolLayer = 0x80000 | 0x20 | 0x80;
				bool overlay = (windowLong & clickThroughToolLayer) == clickThroughToolLayer;
				if (overlay || invisibleLayer || IsTransparentSnapHost(text, windowLong))
				{
					return true;
				}
				if (text == "Windows.UI.Core.CoreWindow")
				{
					if (!inputProcesses.TryGetValue(process, out var value2))
					{
						try
						{
							using (Process process2 = Process.GetProcessById((int)process))
							{
								value2 = process2.ProcessName;
							}
						}
						catch (ArgumentException)
						{
							value2 = "";
						}
						catch (Win32Exception)
						{
							value2 = "";
						}
						inputProcesses[process] = value2;
					}
					if (IsInputBackdrop(text, value2, windowLong))
					{
						return true;
					}
				}
				if (text == "Shell_TrayWnd" || text == "Shell_SecondaryTrayWnd")
				{
					if (Native.GetWindowRect(h, out var r))
					{
						taskbars.Add(new Rectangle(r.Left - desktop.Left, r.Top - desktop.Top, r.Right - r.Left, r.Bottom - r.Top));
					}
					return true;
				}
				if (!Native.GetWindowRect(h, out var r2))
				{
					return true;
				}
				Rectangle rectangle = Rectangle.FromLTRB(r2.Left, r2.Top, r2.Right, r2.Bottom);
				Rectangle rectangle2 = rectangle;
				if (Native.DwmGetWindowAttribute(h, 9, out Native.RECT r3, 16) == 0)
				{
					rectangle2 = Rectangle.FromLTRB(r3.Left, r3.Top, r3.Right, r3.Bottom);
				}
				Rectangle value3;
				bool flag = lastNativeRects.TryGetValue(h, out value3) && value3 == rectangle;
				Rectangle value4;
				bool flag2 = frameInsets.TryGetValue(h, out value4);
				Rectangle rectangle3 = new Rectangle(rectangle2.Left - rectangle.Left, rectangle2.Top - rectangle.Top, rectangle.Right - rectangle2.Right, rectangle.Bottom - rectangle2.Bottom);
				if ((flag || !flag2) && ValidFrameInsets(rectangle3))
				{
					value4 = rectangle3;
					frameInsets[h] = value4;
				}
				rectangle2 = FollowNativeFrame(rectangle, value4);
				live.Add(h);
				lastNativeRects[h] = rectangle;
				Rectangle rectangle4 = rectangle2;
				rectangle4.Offset(-desktop.Left, -desktop.Top);
				if (rectangle4.Width <= 0 || rectangle4.Height <= 0 || !rectangle4.IntersectsWith(new Rectangle(Point.Empty, desktop.Size)))
				{
					return true;
				}
				// Keep our dialogs as snow occluders, without animating over their controls or captions.
				bool shelf = process != processId && CanDecorateWindow(text, Native.GetWindowLong(h, -16), windowLong, rectangle4);
				candidates.Add(new WindowInfo(h.ToInt64().ToString(), rectangle4, shelf, Native.IsZoomed(h))
				{
					SuppressScreen = (h == foreground)
				});
				return true;
			}, IntPtr.Zero);
			foreach (IntPtr item in new List<IntPtr>(lastNativeRects.Keys))
			{
				if (!live.Contains(item))
				{
					lastNativeRects.Remove(item);
					frameInsets.Remove(item);
				}
			}
			world.SetTaskbars(taskbars);
			world.SetWindows(candidates);
		}

	}
}
