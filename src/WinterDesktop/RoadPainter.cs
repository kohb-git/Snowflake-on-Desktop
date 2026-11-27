using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace WinterDesktop
{
	internal static class RoadPainter
	{
		internal static void Draw(Graphics g, Snowplow scene)
		{
			if (scene.Kind == RoadEvent.Plow) { DrawPlow(g, scene); return; }
			if (scene.Kind == RoadEvent.Car) {
				if (scene.Stuck) g.TranslateTransform((float)Math.Sin(scene.StuckTime*24)*1.4f,1);
				g.FillPolygon(Brushes.SteelBlue,new[]{new Point(-27,-12),new Point(-20,-21),new Point(-12,-30),new Point(8,-30),new Point(18,-20),new Point(29,-17),new Point(30,-8),new Point(-28,-8)});
				g.FillPolygon(Brushes.LightCyan,new[]{new Point(-16,-21),new Point(-10,-27),new Point(6,-27),new Point(13,-21)});
				Wheel(g,-17,7); Wheel(g,18,7);
				g.FillRectangle(Brushes.LemonChiffon,24,-17,5,3);
				return;
			}
			// Tractor with front auger and delivery chute, tipper following behind.
			g.FillRectangle(Brushes.ForestGreen,-26,-24,51,14);
			g.FillRectangle(Brushes.DarkOliveGreen,-20,-42,22,21);
			g.FillRectangle(Brushes.LightBlue,-17,-39,15,14);
			g.FillRectangle(Brushes.DarkSlateGray,-23,-44,29,4);
			g.FillRectangle(Brushes.DimGray,13,-35,4,12);
			Wheel(g,-16,13); Wheel(g,18,8);
			g.DrawLine(Pens.DarkSlateGray,21,-13,38,-8);
			g.FillPolygon(Brushes.OrangeRed,new[]{new Point(33,-17),new Point(45,-22),new Point(49,-2),new Point(35,-2)});
			g.DrawLines(Pens.SlateGray,new[]{new Point(38,-18),new Point(28,-40),new Point(-12,-51),new Point(-30,-48)});
			g.FillRectangle(Brushes.DarkSlateGray,-135,-13,86,7);
			g.FillRectangle(Brushes.Firebrick,-80,-32,25,21);
			g.FillRectangle(Brushes.LightSteelBlue,-75,-29,17,10);
			g.FillPolygon(Brushes.DarkOrange,new[]{new Point(-140,-34),new Point(-84,-34),new Point(-88,-15),new Point(-134,-15)});
			if(scene.Load>0) {
				int height=(int)Math.Min(15,scene.Load/12);
				g.FillEllipse(Brushes.WhiteSmoke,-136,-35-height,49,2+height);
			}
			Wheel(g,-125,7); Wheel(g,-108,7); Wheel(g,-63,7);
		}
		private static void Wheel(Graphics g,int x,int r)
		{
			g.FillEllipse(Brushes.DarkSlateGray,x-r,-2*r,2*r,2*r);
			g.FillEllipse(Brushes.Silver,x-r/2,-r-r/2,r,r);
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

		  }
}
