using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinterDesktop
{
	public sealed partial class World
	{
		internal void SetScreens(List<ScreenArea> screens)
		{
			windowSnapshot = null;
			LayoutVersion++;
			Screens = screens;
			Taskbars.Clear();
			Flakes.Clear();
			Plows.Clear();
			Spray.Clear();
			Surfaces.RemoveAll((Surface s) => s.Ground);
			foreach (ScreenArea screen in screens)
			{
				Rectangle snowArea = screen.SnowArea;
				Surface surface = new Surface("ground:" + screen.Id, new Rectangle(snowArea.Left, snowArea.Bottom, snowArea.Width, 2), ground: true);
				surface.Accumulates = AccumulateGround;
				Surfaces.Add(surface);
				Plows.Add(new Snowplow(surface, screen));
				int num = FlakeCount(screen);
				for (int i = 0; i < num; i++)
				{
					Flakes.Add(NewFlake(screen, scatter: true));
				}
			}
			UpdateAnchors();
		}

		internal void SetTaskbars(List<Rectangle> taskbars)
		{
			bool flag = Taskbars.Count != taskbars.Count;
			if (!flag)
			{
				for (int i = 0; i < taskbars.Count; i++)
				{
					if (Taskbars[i] != taskbars[i])
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag) return;
			windowSnapshot = null;
			Taskbars.Clear();
			Taskbars.AddRange(taskbars);
			foreach (ScreenArea screen in Screens)
			{
				Rectangle snowArea = Rectangle.Intersect(screen.Bounds, screen.Work);
				foreach (Rectangle taskbar in taskbars)
				{
					Rectangle rectangle = Rectangle.Intersect(screen.Bounds, taskbar);
					if (rectangle.Width >= screen.Bounds.Width / 2 && rectangle.Height > 0 && rectangle.Top > screen.Bounds.Top + screen.Bounds.Height / 2)
					{
						snowArea.Height = Math.Max(0, Math.Min(snowArea.Bottom, rectangle.Top) - snowArea.Top);
					}
				}
				screen.SnowArea = snowArea;
				foreach (Surface surface in Surfaces)
				{
					if (surface.Id == "ground:" + screen.Id)
					{
						surface.Resize(new Rectangle(snowArea.Left, snowArea.Bottom, snowArea.Width, 2));
					}
				}
			}
			UpdateAnchors();
		}

		private Flake NewFlake(ScreenArea screen, bool scatter)
		{
			Rectangle snowArea = screen.SnowArea;
			Flake flake = new Flake();
			flake.Screen = screen;
			flake.X = (float)snowArea.Left + (float)random.NextDouble() * (float)snowArea.Width;
			flake.Y = (float)(snowArea.Top - 8) + (scatter ? ((float)random.NextDouble() * (float)snowArea.Height) : 0f);
			flake.Speed = 25f + (float)random.NextDouble() * 65f;
			flake.Radius = 0.8f + (float)random.NextDouble() * 1.5f;
			flake.Phase = (float)random.NextDouble() * 6.28f;
			flake.Alpha = 135 + random.Next(110);
			return flake;
		}

		public void SetWindows(List<WindowInfo> infos)
		{
			bool flag = windowSnapshot != null && windowSnapshot.Count == infos.Count;
			if (flag)
			{
				for (int i = 0; i < infos.Count; i++)
				{
					WindowInfo windowInfo = windowSnapshot[i];
					WindowInfo windowInfo2 = infos[i];
					if (windowInfo.Id != windowInfo2.Id || windowInfo.Bounds != windowInfo2.Bounds || windowInfo.Shelf != windowInfo2.Shelf || windowInfo.Maximized != windowInfo2.Maximized || windowInfo.SuppressScreen != windowInfo2.SuppressScreen)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				return;
			}
			windowSnapshot = new List<WindowInfo>();
			foreach (WindowInfo info in infos)
			{
				windowSnapshot.Add(new WindowInfo(info.Id, info.Bounds, info.Shelf, info.Maximized)
				{
					SuppressScreen = info.SuppressScreen
				});
			}
			LayoutVersion++;
			Dictionary<string, Surface> dictionary = new Dictionary<string, Surface>();
			foreach (Surface surface in Surfaces)
			{
				if (!surface.Ground)
				{
					dictionary[surface.Id] = surface;
				}
			}
			List<Surface> list = new List<Surface>();
			List<Rectangle> list2 = new List<Rectangle>();
			List<string> list3 = new List<string>();
			WindowCovers.Clear();
			WindowRects.Clear();
			FullScreenCovers.Clear();
			foreach (WindowInfo info2 in infos)
			{
				WindowCovers.Add(info2.Bounds);
				WindowRects[info2.Id] = info2.Bounds;
				if (info2.Shelf && info2.SuppressScreen)
				{
					foreach (ScreenArea screen in Screens)
					{
						Rectangle bounds = screen.Bounds;
						Rectangle bounds2 = info2.Bounds;
						if (bounds.Width > 0 && bounds.Height > 0 && bounds2.Left <= bounds.Left + 1 && bounds2.Top <= bounds.Top + 1 && bounds2.Right >= bounds.Right - 1 && bounds2.Bottom >= bounds.Bottom - 1)
						{
							FullScreenCovers.Add(bounds);
						}
					}
				}
				if (CanCollectSnow(info2))
				{
					if (!dictionary.TryGetValue(info2.Id, out var value))
					{
						value = new Surface(info2.Id, info2.Bounds, ground: false);
					}
					else
					{
						Rectangle bounds3 = value.Bounds;
						if (MotionEffectsEnabled)
						{
							value.NudgeGarland((float)(info2.Bounds.X - bounds3.X) * SwayStrength, (float)(info2.Bounds.Y - bounds3.Y) * SwayStrength, (float)(info2.Bounds.Width - bounds3.Width) * SwayStrength);
						}
						if (MotionEffectsEnabled && bounds3.Width != info2.Bounds.Width)
						{
							value.GarlandVelocity = Math.Max(-90f, Math.Min(90f, value.GarlandVelocity + (float)(info2.Bounds.Width - bounds3.Width) * 0.3f * SwayStrength));
						}
						if (bounds3.Width != info2.Bounds.Width)
						{
							ResizeSnow(value, info2.Bounds);
						}
						else
						{
							value.Resize(info2.Bounds);
						}
						if (MotionEffectsEnabled && bounds3.Size == info2.Bounds.Size)
						{
							Shake(value, bounds3);
						}
					}
					value.Accumulates = AccumulateWindows;
					dictionary.Remove(info2.Id);
					value.Covers = new List<Rectangle>(list2);
					value.FrontWindows = new List<string>(list3);
					list.Add(value);
				}
				list2.Add(info2.Bounds);
				list3.Add(info2.Id);
			}
			foreach (Surface value2 in dictionary.Values)
			{
				Shed(value2);
			}
			foreach (Surface surface2 in Surfaces)
			{
				if (surface2.Ground)
				{
					surface2.Covers = new List<Rectangle>(list2);
					surface2.FrontWindows = new List<string>(list3);
					list.Add(surface2);
				}
			}
			Surfaces.Clear();
			Surfaces.AddRange(list);
			UpdateAnchors();
		}

		private bool CanCollectSnow(WindowInfo info)
		{
			if (!info.Shelf || info.Maximized)
			{
				return false;
			}
			foreach (ScreenArea screen in Screens)
			{
				Rectangle snowArea = screen.SnowArea;
				if (info.Bounds.Right > snowArea.Left && info.Bounds.Left < snowArea.Right && info.Bounds.Top >= snowArea.Top - 2 && info.Bounds.Top <= snowArea.Top + 2)
				{
					return false;
				}
			}
			return true;
		}

	}
}
