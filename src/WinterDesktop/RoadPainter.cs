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
				if(scene.Rescue>=Snowplow.RescuePhase.Approach){
                    float offset=scene.TractorX-scene.X;
                    float rear=scene.Ground.HeightAt(scene.TractorX-20);
                    float front=scene.Ground.HeightAt(scene.TractorX+19);
                    float slope=(rear-front)/39f;
                    float height=rear-20*slope;
                    float angle=(float)Math.Atan(slope);
                    if(scene.Rescue!=Snowplow.RescuePhase.Approach){
                        float sag=scene.Rescue==Snowplow.RescuePhase.Hitch ? Math.Max(0,89-offset) : 0;
                        float hitchX=offset-32*(float)Math.Cos(angle)+12*(float)Math.Sin(angle);
                        float hitchY=-height-32*(float)Math.Sin(angle)-12*(float)Math.Cos(angle);
                        g.DrawBezier(Pens.SlateGray,28,-10-scene.Lift,38,-10+sag-scene.Lift,hitchX-8,hitchY+sag,hitchX,hitchY);
                    }
                    var tractorState=g.Save();
                    g.TranslateTransform(offset,-height);
                    g.RotateTransform(angle*180/(float)Math.PI);
                    g.FillRectangle(Brushes.Firebrick,-32,-21,63,13);
                    g.FillPolygon(Brushes.DarkRed,new[]{new Point(-10,-21),new Point(-9,-34),new Point(7,-34),new Point(19,-21)});
                    g.FillPolygon(Brushes.LightSteelBlue,new[]{new Point(-6,-23),new Point(-6,-31),new Point(6,-31),new Point(14,-23)});
                    g.DrawLine(Pens.Silver,-32,-22,-13,-22);
                    g.FillRectangle(Brushes.LemonChiffon,26,-19,5,4);
                    Wheel(g,-20,7); Wheel(g,19,7);
                    g.Restore(tractorState);
                }
                var carState=g.Save();
                g.TranslateTransform(0,-scene.Lift);
                g.RotateTransform(scene.Pitch);
				g.FillPolygon(Brushes.SteelBlue,new[]{new Point(-27,-12),new Point(-20,-21),new Point(-12,-30),new Point(8,-30),new Point(18,-20),new Point(29,-17),new Point(30,-8),new Point(-28,-8)});
				g.FillPolygon(Brushes.LightCyan,new[]{new Point(-16,-21),new Point(-10,-27),new Point(6,-27),new Point(13,-21)});
				Wheel(g,-17,7); Wheel(g,18,7);
                if (scene.Wheelspin){
                    double angle=scene.StuckTime*22;
                    float dx=4*(float)Math.Cos(angle), dy=4*(float)Math.Sin(angle);
                    g.DrawLine(Pens.LightSteelBlue,-17-dx,-7-dy,-17+dx,-7+dy);
                }
				g.FillRectangle(Brushes.LemonChiffon,24,-17,5,3);
                if(scene.Rescue!=Snowplow.RescuePhase.None && scene.Rescue!=Snowplow.RescuePhase.Tow &&
                    (int)(scene.StuckTime*2)%2==0){
                    g.FillRectangle(Brushes.Orange,-27,-15,4,3);
                    g.FillRectangle(Brushes.Orange,25,-14,4,3);
                }
                g.Restore(carState);
                if(scene.Rescue>=Snowplow.RescuePhase.Exit && scene.Rescue<=Snowplow.RescuePhase.Hitch)
                    DrawDriver(g,scene);
				return;
			}
			DrawTipper(g, scene);
			DrawTractor(g, scene.Travel);
		}

		private static void DrawDriver(Graphics g,Snowplow scene)
        {
            float progress=scene.Rescue==Snowplow.RescuePhase.Exit ? Math.Min(1,scene.hitchTime) :
                scene.Rescue==Snowplow.RescuePhase.Hitch ? Math.Max(0,1-scene.hitchTime) : 1;
            var state=g.Save();
            g.TranslateTransform(-5-32*progress,-scene.Lift*(1-progress));
            float stride=(float)Math.Sin(progress*Math.PI*4)*3;
            g.DrawLine(Pens.DarkSlateGray,-1,-9,-2-stride,-1);
            g.DrawLine(Pens.DarkSlateGray,1,-9,2+stride,-1);
            g.FillRectangle(Brushes.DarkOrange,-3,-17,7,9);
            g.FillEllipse(Brushes.Bisque,-3,-23,6,7);
            g.FillRectangle(Brushes.MidnightBlue,-3,-24,7,3);
            if(scene.Rescue==Snowplow.RescuePhase.Call){
                g.DrawLine(Pens.Orange,2,-15,6,-13);
                g.DrawLine(Pens.Bisque,6,-13,4,-19);
                g.FillRectangle(Brushes.Black,3,-21,2,4);
            }
            g.Restore(state);
        }

        private static void DrawTractor(Graphics g, float travel)
		{
			// The chassis, glazed cab and loader linkage share fixed axle centres.
			g.FillRectangle(Brushes.DarkSlateGray,-25,-16,55,9);
			using(var hood=new LinearGradientBrush(new Rectangle(-2,-31,35,18),Color.OliveDrab,Color.DarkOliveGreen,90f))
				g.FillPolygon(hood,new[]{new Point(-3,-30),new Point(20,-30),new Point(30,-25),new Point(29,-16),new Point(-3,-16)});
			g.DrawLine(Pens.Goldenrod,0,-26,26,-24);
			for(int x=18;x<27;x+=3)g.DrawLine(Pens.DarkSlateGray,x,-23,x,-18);
			g.FillRectangle(Brushes.DimGray,8,-42,3,13);
			g.DrawLine(Pens.Silver,8,-40,8,-32);
			g.FillPolygon(Brushes.DarkSlateGray,new[]{new Point(-31,-46),new Point(-8,-46),new Point(0,-27),new Point(-2,-19),new Point(-30,-19)});
			using(var glass=new LinearGradientBrush(new Point(-28,-43),new Point(-9,-23),Color.LightSteelBlue,Color.DarkSlateGray)){
				g.FillPolygon(glass,new[]{new Point(-27,-43),new Point(-19,-43),new Point(-19,-28),new Point(-28,-27)});
				g.FillPolygon(glass,new[]{new Point(-16,-43),new Point(-10,-43),new Point(-4,-28),new Point(-16,-28)});
			}
			g.DrawLine(Pens.LightGray,-11,-28,-5,-34);
			g.FillRectangle(Brushes.ForestGreen,-33,-48,28,4);
			g.DrawLine(Pens.WhiteSmoke,-31,-48,-10,-48);
			g.FillRectangle(Brushes.DarkOrange,-29,-51,4,3);
			g.DrawLine(Pens.DarkSlateGray,-6,-40,2,-42);g.FillRectangle(Brushes.Black,1,-44,3,6);
			g.FillRectangle(Brushes.DimGray,-8,-17,8,3);g.FillRectangle(Brushes.Silver,-8,-12,7,2);
			Tire(g,-22,14,travel,true); Tire(g,21,9,travel,true);
			using(var fender=new Pen(Color.OliveDrab,3))g.DrawArc(fender,-38,-30,32,31,194,138);
			using(var arm=new Pen(Color.SlateGray,3))g.DrawLine(arm,12,-20,39,-9);
			g.DrawLine(Pens.Silver,22,-22,41,-12);
			g.FillPolygon(Brushes.Firebrick,new[]{new Point(34,-18),new Point(46,-23),new Point(52,-20),new Point(52,-3),new Point(34,-3)});
			g.FillEllipse(Brushes.DarkSlateGray,39,-18,12,14);
			g.DrawArc(Pens.Silver,40,-17,9,12,(int)(travel*5)%360,240);
			g.DrawLine(Pens.Gainsboro,34,-3,52,-3);
			var chute=new[]{new Point(39,-15),new Point(28,-37),new Point(4,-49),new Point(-30,-55),new Point(-48,-51)};
			using(var tube=new Pen(Color.DarkSlateGray,5)){tube.LineJoin=LineJoin.Round;g.DrawLines(tube,chute);}
			g.DrawLines(Pens.LightSlateGray,chute);
			g.FillRectangle(Brushes.LightYellow,26,-25,4,3);
		}

		private static void DrawTipper(Graphics g, Snowplow scene)
		{
			g.FillRectangle(Brushes.DarkSlateGray,-155,-15,101,7);
			g.FillRectangle(Brushes.Gray,-107,-13,16,6);
			g.FillPolygon(Brushes.DarkSlateGray,new[]{new Point(-87,-36),new Point(-65,-36),new Point(-55,-25),new Point(-55,-12),new Point(-87,-12)});
			using(var cab=new LinearGradientBrush(new Point(-85,-35),new Point(-65,-12),Color.Peru,Color.Sienna))
				g.FillPolygon(cab,new[]{new Point(-85,-35),new Point(-66,-35),new Point(-57,-25),new Point(-57,-15),new Point(-85,-15)});
			g.FillPolygon(Brushes.LightSteelBlue,new[]{new Point(-80,-32),new Point(-68,-32),new Point(-65,-25),new Point(-80,-25)});
			g.FillPolygon(Brushes.DarkSlateGray,new[]{new Point(-66,-32),new Point(-59,-25),new Point(-63,-25)});
			g.DrawRectangle(Pens.SaddleBrown,-82,-24,16,9);
			g.DrawLine(Pens.Black,-79,-22,-75,-22);g.FillRectangle(Brushes.LightYellow,-60,-19,4,3);
			g.FillRectangle(Brushes.SlateGray,-62,-13,9,3);g.FillRectangle(Brushes.Silver,-86,-12,10,2);
			using(var bed=new LinearGradientBrush(new Point(-150,-39),new Point(-145,-18),Color.DarkGoldenrod,Color.SaddleBrown))
				g.FillPolygon(bed,new[]{new Point(-158,-38),new Point(-91,-38),new Point(-94,-17),new Point(-153,-17)});
			for(int x=-149;x<-98;x+=13){g.DrawLine(Pens.Sienna,x,-35,x+3,-20);g.DrawLine(Pens.Peru,x+1,-35,x+4,-20);}
			if(scene.Load>0){
				int h=(int)Math.Min(12,scene.Load/15f);
				g.FillPolygon(Brushes.WhiteSmoke,new[]{new Point(-154,-38),new Point(-145,-38-h/2),new Point(-134,-38-h),new Point(-124,-37-h),new Point(-115,-39-h/2),new Point(-101,-38-h/3),new Point(-94,-38)});
			}
			g.DrawLine(Pens.Wheat,-157,-38,-91,-38);
			Tire(g,-144,8,scene.Travel,false);Tire(g,-123,8,scene.Travel,false);Tire(g,-70,8,scene.Travel,false);
			g.FillRectangle(Brushes.Red,-156,-17,3,3);
		}

		private static void Tire(Graphics g,int x,int radius,float travel,bool tractor)
		{
			int cy=-radius;
			g.FillEllipse(Brushes.Black,x-radius,cy-radius,radius*2,radius*2);
			float angle=travel/radius;
			int count=tractor?12:8;
			for(int i=0;i<count;i++) {
				double a=angle+i*2*Math.PI/count;
				float dx=(float)Math.Cos(a),dy=(float)Math.Sin(a);
				g.DrawLine(Pens.DimGray,x+dx*(radius-3)-dy,cy+dy*(radius-3)+dx,x+dx*(radius-1),cy+dy*(radius-1));
			}
			int hub=radius/2;
			g.FillEllipse(tractor?Brushes.Goldenrod:Brushes.LightSlateGray,x-hub,cy-hub,hub*2,hub*2);
			g.FillEllipse(Brushes.DarkSlateGray,x-2,cy-2,4,4);
			for(int i=0;i<5;i++){
				double a=angle+2*Math.PI*i/5;
				g.FillEllipse(Brushes.Gainsboro,x+(float)Math.Cos(a)*(hub-1)-0.65f,cy+(float)Math.Sin(a)*(hub-1)-0.65f,1.3f,1.3f);
			}
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


