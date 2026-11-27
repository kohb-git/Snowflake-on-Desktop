using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WinterDesktop
{
    internal static class LampSprites
    {
        // Six palette colours, with at most 33 brightness levels for each.
        private static readonly Dictionary<long, Bitmap> Cache = new Dictionary<long, Bitmap>();
        static LampSprites()
        {
            Application.ApplicationExit += delegate {
                foreach (var sprite in Cache.Values) sprite.Dispose();
                Cache.Clear();
            };
        }
        internal static void Draw(Graphics target, Color color, float brightness, float x, float y, float scale, float angle)
        {
            int level = Math.Max(0, Math.Min(32, (int)Math.Round(brightness * 32)));
            long key = ((long)color.ToArgb() << 8) | (byte)level;
            Bitmap sprite;
            if (!Cache.TryGetValue(key, out sprite)) {
                sprite = Make(color, level / 32f);
                Cache.Add(key, sprite);
            }
            if (scale == 1f && Math.Abs(angle) < 0.05f) {
                target.DrawImageUnscaled(sprite,(int)Math.Round(x)-16,(int)Math.Round(y)-9);
                return;
            }
            var state=target.Save();
            target.TranslateTransform(x,y);
            target.RotateTransform(angle);
            target.ScaleTransform(scale,scale);
            target.DrawImage(sprite,new RectangleF(-16,-9,32,34),new RectangleF(0,0,32,34),GraphicsUnit.Pixel);
            target.Restore(state);
        }
        private static Bitmap Make(Color color, float light)
        {
            var bitmap=new Bitmap(32,34,PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(bitmap))
            using(var path=new GraphicsPath()) {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                path.AddEllipse(1,1,30,32);
                using(var glow=new PathGradientBrush(path)) {
                    glow.CenterColor=Color.FromArgb((int)(165*light),color);
                    glow.SurroundColors=new[]{Color.FromArgb(0,color)};
                    g.FillPath(glow,path);
                }
                using(var socket=new SolidBrush(Color.FromArgb(44,78,66)))
                using(var bulb=new SolidBrush(Color.FromArgb(Channel(color.R,light),Channel(color.G,light),Channel(color.B,light))))
                using(var glint=new SolidBrush(Color.FromArgb((int)(245*light),255,252,239))) {
                    g.FillRectangle(socket,14,9,4,4);
                    g.FillEllipse(bulb,13,12,6,8);
                    g.FillEllipse(glint,14.5f,13,2.5f,3.5f);
                }
            }
            return bitmap;
        }
        private static int Channel(int channel,float light) { return (int)((channel+(255-channel)*0.12f)*light); }
    }
}

