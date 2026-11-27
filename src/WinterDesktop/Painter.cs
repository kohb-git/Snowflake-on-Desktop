using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WinterDesktop
{
	internal static class Painter
	{
		private static readonly Color[] BulbColors = new Color[4]
		{
			Color.FromArgb(255, 104, 95),
			Color.FromArgb(255, 211, 112),
			Color.FromArgb(103, 225, 173),
			Color.FromArgb(117, 185, 255)
		};

		internal static void Draw(Graphics g, World world, Surface separate = null)
		{
			g.ResetTransform();
			g.ResetClip();
			g.CompositingMode = CompositingMode.SourceCopy;
			g.Clear(Color.Transparent);
			g.CompositingMode = CompositingMode.SourceOver;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			using (Region region = new Region())
			{
				region.MakeEmpty();
				foreach (ScreenArea screen in world.Screens)
				{
					region.Union(screen.SnowArea);
				}
				foreach (Rectangle taskbar in world.Taskbars)
				{
					region.Exclude(taskbar);
				}
				foreach (Rectangle fullScreenCover in world.FullScreenCovers)
				{
					region.Exclude(fullScreenCover);
				}
				g.SetClip(region, CombineMode.Replace);
			}
			var allEffects = g.Save();
			ClipEffects(g, world, false);
			using (SolidBrush solidBrush = new SolidBrush(Color.White))
			{
				foreach (ScreenArea screen2 in world.Screens)
				{
					GraphicsState gstate = g.Save();
					g.SetClip(screen2.SnowArea, CombineMode.Intersect);
					foreach (Rectangle windowCover in world.WindowCovers)
					{
						g.ExcludeClip(windowCover);
					}
					foreach (Flake flake in world.Flakes)
					{
						if (flake.Screen == screen2 && !(flake.Y + flake.Radius < (float)screen2.SnowArea.Top) && !(flake.Y - flake.Radius >= (float)screen2.SnowArea.Bottom))
						{
							solidBrush.Color = Color.FromArgb((int)flake.Alpha, 255, 255, 255);
							g.FillEllipse(solidBrush, flake.X - flake.Radius, flake.Y - flake.Radius, flake.Radius * 2f, flake.Radius * 2f);
						}
					}
					g.Restore(gstate);
				}
			}
			g.Restore(allEffects);
			var lightsState = g.Save();
			ClipEffects(g, world, true);
			if (world.GarlandEnabled || world.Display.Modes.Count > 0)
			{
				foreach (Surface surface2 in world.Surfaces)
				{
					if (!surface2.Ground && surface2 != separate)
					{
						GraphicsState gstate2 = g.Save();
						Clip(g, surface2);
						g.SetClip(new Rectangle(surface2.Bounds.Left, surface2.Bounds.Top, surface2.Bounds.Width, 64), CombineMode.Intersect);
						DrawGarland(g, surface2, world.AnimationTime, world);
						g.Restore(gstate2);
					}
				}
			}
			g.Restore(lightsState);
			ClipEffects(g, world, false);
			foreach (Surface surface3 in world.Surfaces)
			{
				GraphicsState gstate3 = g.Save();
				Clip(g, surface3);
				DrawDrift(g, surface3);
				g.Restore(gstate3);
			}
			Dictionary<List<string>, List<LooseSnow>> dictionary = new Dictionary<List<string>, List<LooseSnow>>();
			foreach (LooseSnow item in world.Loose)
			{
				if (!dictionary.TryGetValue(item.FrontWindows, out var value))
				{
					value = new List<LooseSnow>();
					dictionary.Add(item.FrontWindows, value);
				}
				value.Add(item);
			}
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(245, 250, 255)))
			{
				foreach (KeyValuePair<List<string>, List<LooseSnow>> item2 in dictionary)
				{
					GraphicsState gstate4 = g.Save();
					foreach (string item3 in item2.Key)
					{
						if (world.WindowRects.TryGetValue(item3, out var value2))
						{
							g.ExcludeClip(value2);
						}
					}
					foreach (LooseSnow item4 in item2.Value)
					{
						g.FillEllipse(brush, item4.X - item4.Radius, item4.Y - item4.Radius, item4.Radius * 2f, item4.Radius * 2f);
					}
					g.Restore(gstate4);
				}
			}
			using (SolidBrush solidBrush2 = new SolidBrush(Color.White))
			{
				foreach (Powder item5 in world.Dust)
				{
					GraphicsState gstate5 = g.Save();
					foreach (string frontWindow in item5.FrontWindows)
					{
						if (world.WindowRects.TryGetValue(frontWindow, out var value3))
						{
							g.ExcludeClip(value3);
						}
					}
					solidBrush2.Color = Color.FromArgb((int)(210f * Math.Max(0f, 1f - item5.Age / item5.Life)), 250, 252, 255);
					g.FillEllipse(solidBrush2, item5.X - item5.Radius, item5.Y - item5.Radius, item5.Radius * 2f, item5.Radius * 2f);
					g.Restore(gstate5);
				}
			}
			using (SolidBrush solidBrush3 = new SolidBrush(Color.White))
			{
				foreach (PlowSpray item6 in world.Spray)
				{
					GraphicsState gstate6 = g.Save();
					g.SetClip(item6.Screen.SnowArea, CombineMode.Intersect);
					solidBrush3.Color = Color.FromArgb((int)(220f * Math.Max(0f, 1f - item6.Age / item6.Life)), 255, 255, 255);
					g.FillEllipse(solidBrush3, item6.X - item6.Radius, item6.Y - item6.Radius, item6.Radius * 2f, item6.Radius * 2f);
					g.Restore(gstate6);
				}
			}
			foreach (Snowplow plow in world.Plows)
			{
				if (plow.Active)
				{
					GraphicsState gstate7 = g.Save();
					g.SetClip(plow.Screen.SnowArea, CombineMode.Intersect);
					g.TranslateTransform(plow.X, plow.Ground.Bounds.Top - 1);
					DrawPlow(g, plow);
					g.Restore(gstate7);
				}
			}
			foreach (Ball ball in world.Balls)
			{
				GraphicsState gstate8 = g.Save();
				Surface surface = world.Anchor(ball);
				if (surface != null)
				{
					Clip(g, surface);
				}
				DrawBall(g, ball);
				g.Restore(gstate8);
			}
		}

		private static void ClipEffects(Graphics g, World world, bool lights)
		{
			foreach (var screen in world.Screens)
				if (lights ? !world.Display.LightsOn(screen.Id) : !world.Display.SnowOn(screen.Id))
					g.ExcludeClip(screen.Bounds);
		}

		private static float WireY(float t, float top, float sag)
		{
			return top + 3f + sag * 4f * t * (1f - t);
		}

		internal static float GarlandBrightness(double time, int bulb)
		{
			return 0.58f + 0.42f * (float)(0.5 + 0.5 * Math.Sin(time * 1.7 + (double)bulb * 0.72));
		}

		internal static void DrawGarland(Graphics g, Surface surface, double time, World world)
		{
			_ = surface.Bounds;
			int garlandSpans = surface.GarlandSpans;
			using (Pen pen = new Pen(Color.FromArgb(220, 39, 76, 65), 1.4f))
			{
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(44, 78, 66)))
				{
					for (int i = 0; i < garlandSpans; i++)
					{
						PointF[] array = new PointF[25];
						for (int j = 0; j < array.Length; j++)
						{
							float t = (float)j / 24f;
							ref PointF reference = ref array[j];
							reference = surface.CordPoint(i, t, world.Display);
						}
						g.DrawLines(pen, array);
						int num = 6;
						for (int k = 0; k < num; k++)
						{
							float num2 = ((float)k + 0.5f) / (float)num;
							PointF pointF = surface.CordPoint(i, num2, world.Display);
							float x = pointF.X;
							float y = pointF.Y;
							GraphicsState gstate = g.Save();
							g.TranslateTransform(x, y);
							g.ScaleTransform(world.LampScale, world.LampScale);
							g.RotateTransform(Math.Max(-35f, Math.Min(35f, (0f - surface.CordSwaySpeed[i]) * 0.45f + surface.GarlandSwing * (float)Math.Cos((double)((float)i + num2) * 0.8))));
							g.TranslateTransform(0f - x, 0f - y);
							Color baseColor = ((world.GarlandTheme == 1) ? Color.FromArgb(255, 221, 152) : ((world.GarlandTheme == 2) ? Color.FromArgb(165, 222, 255) : BulbColors[(i * num + k) % BulbColors.Length]));
							float num3 = ((world.TwinkleSpeed == 0f) ? 1f : GarlandBrightness(time * (double)world.TwinkleSpeed, i * num + k)) * world.LightLevel;
							using (GraphicsPath graphicsPath = new GraphicsPath())
							{
								graphicsPath.AddEllipse(x - 15f, y - 8f, 30f, 32f);
								using (PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath))
								{
									pathGradientBrush.CenterColor = Color.FromArgb((int)(165f * num3), baseColor);
									pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, baseColor) };
									g.FillPath(pathGradientBrush, graphicsPath);
								}
							}
							g.FillRectangle(brush, x - 2f, y, 4f, 4f);
							using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(255, (int)(((float)(int)baseColor.R + (float)(255 - baseColor.R) * 0.12f) * num3), (int)(((float)(int)baseColor.G + (float)(255 - baseColor.G) * 0.12f) * num3), (int)(((float)(int)baseColor.B + (float)(255 - baseColor.B) * 0.12f) * num3))))
							{
								g.FillEllipse(brush2, x - 3f, y + 3f, 6f, 8f);
							}
							using (SolidBrush brush3 = new SolidBrush(Color.FromArgb((int)(245f * num3), 255, 252, 239)))
							{
								g.FillEllipse(brush3, x - 1.5f, y + 4f, 2.5f, 3.5f);
							}
							g.Restore(gstate);
						}
					}
				}
			}
		}

		private static void Clip(Graphics g, Surface s)
		{
			foreach (Rectangle cover in s.Covers)
			{
				g.ExcludeClip(cover);
			}
		}

		private static void DrawPlow(Graphics g, Snowplow plow)
		{
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(45, 18, 28, 44)))
			{
				g.FillEllipse(brush, -45, -5, 103, 5);
			}
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(47, 58, 70)))
			{
				g.FillRectangle(brush2, -39, -16, 76, 8);
				g.FillRectangle(brush2, 28, -12, 15, 5);
			}
			using (LinearGradientBrush brush3 = new LinearGradientBrush(new Point(0, -43), new Point(0, -14), Color.FromArgb(255, 194, 64), Color.FromArgb(224, 127, 31)))
			{
				g.FillRectangle(brush3, -41, -38, 38, 21);
				g.FillPolygon(brush3, new Point[5]
				{
					new Point(-1, -17),
					new Point(-1, -42),
					new Point(19, -42),
					new Point(29, -29),
					new Point(29, -17)
				});
			}
			using (Pen pen = new Pen(Color.FromArgb(255, 221, 134), 1f))
			{
				g.DrawLine(pen, -39, -37, -5, -37);
				g.DrawLine(pen, 1, -41, 18, -41);
			}
			using (SolidBrush brush4 = new SolidBrush(Color.FromArgb(68, 113, 140)))
			{
				g.FillPolygon(brush4, new Point[5]
				{
					new Point(3, -38),
					new Point(17, -38),
					new Point(24, -29),
					new Point(24, -26),
					new Point(3, -26)
				});
			}
			using (Pen pen2 = new Pen(Color.FromArgb(135, 225, 242, 248), 1.6f))
			{
				g.DrawLine(pen2, 5, -36, 17, -36);
			}
			using (Pen pen3 = new Pen(Color.FromArgb(82, 69, 49), 2f))
			{
				g.DrawLine(pen3, 5, -22, 10, -22);
			}
			using (Pen pen4 = new Pen(Color.FromArgb(200, 110, 30), 1.4f))
			{
				for (int i = 0; i < 3; i++)
				{
					g.DrawLine(pen4, -34 + i * 10, -33, -34 + i * 10, -21);
				}
			}
			using (Pen pen5 = new Pen(Color.FromArgb(69, 80, 91), 3f))
			{
				g.DrawLine(pen5, -31, -38, -31, -46);
				g.DrawLine(pen5, -31, -46, -35, -46);
			}
			using (SolidBrush brush5 = new SolidBrush(Color.FromArgb(255, 248, 202)))
			{
				g.FillRectangle(brush5, 26, -25, 4, 5);
			}
			using (SolidBrush brush6 = new SolidBrush(Color.FromArgb(194, 62, 44)))
			{
				g.FillRectangle(brush6, -43, -23, 3, 5);
			}
			float[] array = new float[2] { -25f, 20f };
			foreach (float num in array)
			{
				g.FillEllipse(Brushes.DarkSlateGray, num - 9f, -18f, 18f, 18f);
				using (SolidBrush brush7 = new SolidBrush(Color.FromArgb(33, 41, 51)))
				{
					g.FillEllipse(brush7, num - 8f, -17f, 16f, 16f);
				}
				using (SolidBrush brush8 = new SolidBrush(Color.FromArgb(174, 190, 198)))
				{
					g.FillEllipse(brush8, num - 4.5f, -13.5f, 9f, 9f);
				}
				float num2 = plow.Travel / 8f;
				using (Pen pen6 = new Pen(Color.FromArgb(86, 105, 118), 1.5f))
				{
					for (int k = 0; k < 3; k++)
					{
						float num3 = num2 + (float)k * 2.0944f;
						g.DrawLine(pen6, num, -9f, num + (float)Math.Cos(num3) * 4f, -9f + (float)Math.Sin(num3) * 4f);
					}
				}
			}
			using (SolidBrush brush9 = new SolidBrush(Color.FromArgb(83, 98, 108)))
			{
				g.FillRectangle(brush9, 33, -13, 11, 4);
			}
			using (LinearGradientBrush brush10 = new LinearGradientBrush(new Point(40, -25), new Point(56, -2), Color.FromArgb(230, 240, 245), Color.FromArgb(128, 159, 178)))
			{
				g.FillPolygon(brush10, new Point[5]
				{
					new Point(44, -25),
					new Point(51, -24),
					new Point(57, -3),
					new Point(43, -2),
					new Point(40, -7)
				});
			}
			using (Pen pen7 = new Pen(Color.FromArgb(245, 252, 255), 1.5f))
			{
				g.DrawLine(pen7, 44, -25, 51, -24);
			}
			using (Pen pen8 = new Pen(Color.FromArgb(75, 95, 111), 2f))
			{
				g.DrawLine(pen8, 43, -2, 57, -3);
			}
			float num4 = 0.5f + 0.5f * (float)Math.Sin(plow.Travel * 0.09f);
			using (SolidBrush brush11 = new SolidBrush(Color.FromArgb((int)(35f + 45f * num4), 255, 186, 48)))
			{
				g.FillEllipse(brush11, 3, -54, 20, 16);
			}
			using (SolidBrush brush12 = new SolidBrush(Color.FromArgb(255, (int)(151f + 65f * num4), 33)))
			{
				g.FillRectangle(brush12, 10, -48, 6, 5);
			}
			g.FillRectangle(Brushes.DimGray, 8, -43, 10, 2);
		}

		private static void DrawDrift(Graphics g, Surface s)
		{
			bool flag = false;
			float[] snow = s.Snow;
			foreach (float num in snow)
			{
				if (num > 0.12f)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return;
			}
			int num2 = Math.Max(2, s.Snow.Length);
			PointF[] array = new PointF[num2];
			for (int j = 0; j < num2; j++)
			{
				float num3 = (float)(s.Bounds.Left + 3) + (float)((s.Bounds.Width - 6) * j) / (float)(num2 - 1);
				float num4 = (s.HeightAt(num3 - 4f) + s.HeightAt(num3) * 2f + s.HeightAt(num3 + 4f)) / 4f;
				float num5 = Math.Min(1f, (float)Math.Min(j, num2 - 1 - j) / 2f);
				ref PointF reference = ref array[j];
				reference = new PointF(num3, (float)s.Bounds.Top - num4 * num5);
			}
			for (int k = 0; k < num2; k++)
			{
				if (!(array[k].Y >= (float)s.Bounds.Top - 0.12f))
				{
					int num6 = Math.Max(0, k - 1);
					for (; k < num2 && array[k].Y < (float)s.Bounds.Top - 0.12f; k++)
					{
					}
					int num7 = Math.Min(num2 - 1, k);
					PointF[] array2 = new PointF[num7 - num6 + 1];
					Array.Copy(array, num6, array2, 0, array2.Length);
					DrawDriftSegment(g, s, array2);
				}
			}
		}

		private static void DrawDriftSegment(Graphics g, Surface s, PointF[] crest)
		{
			int num = crest.Length;
			if (num < 2)
			{
				return;
			}
			using (GraphicsPath graphicsPath = new GraphicsPath())
			{
				graphicsPath.AddCurve(crest, 0.35f);
				graphicsPath.AddLine(crest[num - 1], new PointF(crest[num - 1].X, (float)s.Bounds.Top + 0.4f));
				graphicsPath.AddLine(crest[num - 1].X, (float)s.Bounds.Top + 0.4f, crest[0].X, (float)s.Bounds.Top + 0.4f);
				graphicsPath.CloseFigure();
				using (LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new PointF(0f, s.Bounds.Top - 48), new PointF(0f, s.Bounds.Top + 2), Color.White, Color.FromArgb(229, 236, 242)))
				{
					linearGradientBrush.InterpolationColors = new ColorBlend
					{
						Positions = new float[3] { 0f, 0.72f, 1f },
						Colors = new Color[3]
						{
							Color.White,
							Color.FromArgb(250, 252, 255),
							Color.FromArgb(224, 232, 240)
						}
					};
					g.FillPath(linearGradientBrush, graphicsPath);
				}
				using (Pen pen = new Pen(Color.FromArgb(210, 255, 255, 255), 1f))
				{
					g.DrawCurve(pen, crest, 0.35f);
				}
			}
		}

		private static void DrawBall(Graphics g, Ball b)
		{
			float r = b.R;
			float x = b.X;
			float y = b.Y;
			if (b.Rest && b.Parent == null)
			{
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(45, 40, 58, 78)))
				{
					g.FillEllipse(brush, x - r * 1.05f, y + r - 3f, r * 2.1f, 5f);
				}
			}
			RectangleF rect = new RectangleF(x - r, y - r, r * 2f, r * 2f);
			using (GraphicsPath graphicsPath = new GraphicsPath())
			{
				graphicsPath.AddEllipse(rect);
				using (PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath))
				{
					pathGradientBrush.CenterPoint = new PointF(x - r * 0.32f, y - r * 0.4f);
					pathGradientBrush.CenterColor = Color.White;
					pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(218, 228, 237) };
					g.FillPath(pathGradientBrush, graphicsPath);
				}
				using (Pen pen = new Pen(Color.FromArgb(150, 255, 255, 255), 1f))
				{
					g.DrawPath(pen, graphicsPath);
				}
			}
			if (b.Level == 1)
			{
				using (Pen pen2 = new Pen(Color.FromArgb(114, 84, 63), 2.3f))
				{
					pen2.StartCap = LineCap.Round;
					pen2.EndCap = LineCap.Round;
					g.DrawLine(pen2, x - r * 0.8f, y - r * 0.1f, x - r * 1.7f, y - r * 0.6f);
					g.DrawLine(pen2, x + r * 0.8f, y - r * 0.1f, x + r * 1.7f, y - r * 0.6f);
					g.DrawLine(pen2, x + r * 1.4f, y - r * 0.43f, x + r * 1.45f, y - r * 0.85f);
				}
				for (int i = -1; i <= 1; i++)
				{
					g.FillEllipse(Brushes.DarkSlateGray, x - 1.8f, y + (float)i * r * 0.4f, 3.6f, 3.6f);
				}
				using (Pen pen3 = new Pen(Color.FromArgb(197, 69, 69), Math.Max(4f, r * 0.24f)))
				{
					pen3.StartCap = LineCap.Round;
					pen3.EndCap = LineCap.Round;
					g.DrawArc(pen3, x - r * 0.76f, y - r * 0.9f, r * 1.52f, r * 0.45f, 10f, 160f);
					g.DrawLine(pen3, x + r * 0.3f, y - r * 0.55f, x + r * 0.45f, y + r * 0.07f);
				}
			}
			if (b.Level == 2)
			{
				float num = Math.Max(2f, r * 0.16f);
				g.FillEllipse(Brushes.DarkSlateGray, x - r * 0.4f, y - r * 0.25f, num, num);
				g.FillEllipse(Brushes.DarkSlateGray, x + r * 0.24f, y - r * 0.25f, num, num);
				g.FillPolygon(Brushes.Coral, new PointF[3]
				{
					new PointF(x, y),
					new PointF(x + r * 0.8f, y + r * 0.08f),
					new PointF(x, y + r * 0.23f)
				});
				using (Pen pen4 = new Pen(Color.FromArgb(87, 103, 117), 1.3f))
				{
					g.DrawArc(pen4, x - r * 0.35f, y + r * 0.13f, r * 0.7f, r * 0.4f, 10f, 155f);
				}
				using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(40, 54, 70)))
				{
					g.FillRectangle(brush2, x - r * 0.6f, y - r * 1.7f, r * 1.2f, r * 0.9f);
					g.FillRectangle(brush2, x - r * 0.9f, y - r * 0.88f, r * 1.8f, r * 0.17f);
				}
				using (SolidBrush brush3 = new SolidBrush(Color.FromArgb(197, 69, 69)))
				{
					g.FillRectangle(brush3, x - r * 0.6f, y - r * 1.08f, r * 1.2f, r * 0.18f);
				}
			}
		}
	}
}


