using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace WinterDesktop
{
	public static class WindowTracking
	{
		private static readonly HashSet<IntPtr> moving = new HashSet<IntPtr>();

		private static long nextScan;

		private static IntPtr foreground;

		public static void Mark(IntPtr window)
		{
			moving.Add(window);
		}

		private static void Scan(SnowWindow form)
		{
			moving.Clear();
			foreground = Native.GetForegroundWindow();
			form.RefreshWindows();
			nextScan = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 8;
		}

		public static bool Refresh(SnowWindow form)
		{
			if (Stopwatch.GetTimestamp() >= nextScan || foreground != Native.GetForegroundWindow() || form.locationHook == IntPtr.Zero)
			{
				Scan(form);
				return true;
			}
			if (moving.Count == 0)
			{
				return false;
			}
			List<WindowInfo> windowSnapshot = form.world.windowSnapshot;
			if (windowSnapshot == null)
			{
				Scan(form);
				return true;
			}
			List<WindowInfo> list = null;
			foreach (IntPtr item in moving)
			{
				if (!form.lastNativeRects.TryGetValue(item, out var value) || !form.frameInsets.TryGetValue(item, out var value2) || !Native.IsWindowVisible(item) || Native.IsIconic(item) || !Native.GetWindowRect(item, out var r))
				{
					Scan(form);
					return true;
				}
				Rectangle rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
				if (!(rectangle == value))
				{
					string id = item.ToInt64().ToString();
					int num = windowSnapshot.FindIndex((WindowInfo candidate) => candidate.Id == id);
					if (num < 0 || rectangle.Size != value.Size)
					{
						Scan(form);
						return true;
					}
					WindowInfo windowInfo = windowSnapshot[num];
					Rectangle rect = SnowWindow.FollowNativeFrame(rectangle, value2);
					Rectangle desktop = form.desktop;
					rect.Offset(-desktop.Left, -desktop.Top);
					if (windowInfo.Maximized != Native.IsZoomed(item) || !new Rectangle(Point.Empty, desktop.Size).IntersectsWith(rect))
					{
						Scan(form);
						return true;
					}
					if (list == null)
					{
						list = new List<WindowInfo>(windowSnapshot);
					}
					WindowInfo windowInfo2 = new WindowInfo(windowInfo.Id, rect, windowInfo.Shelf, windowInfo.Maximized);
					windowInfo2.SuppressScreen = windowInfo.SuppressScreen;
					list[num] = windowInfo2;
					form.lastNativeRects[item] = rectangle;
				}
			}
			moving.Clear();
			if (list == null)
			{
				return false;
			}
			form.world.SetWindows(list);
			return true;
		}

		public static void FrameRefresh(SnowWindow form)
		{
			Refresh(form);
		}

		public static void MoveRefresh(SnowWindow form)
		{
			if (Refresh(form))
			{
				form.Render();
			}
		}
	}
}
