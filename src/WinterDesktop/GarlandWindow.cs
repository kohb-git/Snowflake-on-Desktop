using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinterDesktop
{
    internal sealed class GarlandWindow : Form
    {
        private LayerCanvas strip;
        private IntPtr target;
        private Size rawSize;
        private Point inset;
        internal GarlandWindow() {
            TopMost = true; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams {
            get { var p = base.CreateParams; p.ExStyle |= 0x080800A0; return p; }
        }
        private static bool Fits(SnowWindow form, Rectangle band, Surface surface) {
            if (!form.world.Screens.Exists(s => s.SnowArea.Contains(band))) return false;
            return !surface.Covers.Exists(r => r.IntersectsWith(band)) &&
                !form.world.FullScreenCovers.Exists(r => r.IntersectsWith(band)) &&
                !form.world.Taskbars.Exists(r => r.IntersectsWith(band));
        }
        internal Surface Prepare(SnowWindow form, byte opacity) {
            return PrepareFor(form, opacity, Native.GetForegroundWindow());
        }
        internal Surface PrepareFor(SnowWindow form, byte opacity, IntPtr h) {
            Rectangle desktop = form.desktop;
            if (form.world.GarlandEnabled && form.frameInsets.ContainsKey(h) && !Native.HasWindowCapture(h)) {
                var id = h.ToInt64().ToString();
                var s = form.world.Surfaces.Find(item => !item.Ground && item.Id == id);
                if (s != null && Native.GetWindowRect(h,out var raw)) {
                    var delta = form.frameInsets[h];
                    var band = new Rectangle(raw.Left+delta.X-desktop.X,raw.Top+delta.Y-desktop.Y,s.Bounds.Width,64);
                    if (raw.Right-raw.Left-delta.X-delta.Width == band.Width && Fits(form,band,s)) {
                        if (strip == null || strip.Bitmap.Width != band.Width) {
                            var replacement = new LayerCanvas(band.Width,64);
                            if (strip != null) strip.Dispose();
                            strip = replacement;
                        }
                        var g=strip.Graphics; g.ResetTransform(); g.Clear(Color.Transparent);
                        g.SmoothingMode=SmoothingMode.AntiAlias;
                        g.TranslateTransform(-s.Bounds.X,-s.Bounds.Y);
                        Painter.DrawGarland(g,s,form.world.AnimationTime,form.world);
                        band.Offset(desktop.Location);
                        strip.Present(Handle,band,opacity);
                        if (!Visible) { Bounds=band; Show(form); }
                        target=h; inset=delta.Location; rawSize=new Size(raw.Right-raw.Left,raw.Bottom-raw.Top);
                        Follow(form,h);
                        return s;
                    }
                }
            }
            target=IntPtr.Zero; return null;
        }
        internal void FinishFrame(Surface separate) {
            if (separate == null && Visible) Hide();
        }
        internal void Follow(SnowWindow form, IntPtr h) {
            Rectangle desktop = form.desktop;
            if (h != target || !Visible || !Native.GetWindowRect(h,out var raw)) return;
            if (raw.Right-raw.Left != rawSize.Width || raw.Bottom-raw.Top != rawSize.Height) return;
            var band=new Rectangle(raw.Left+inset.X-desktop.X,raw.Top+inset.Y-desktop.Y,strip.Bitmap.Width,64);
            var id=h.ToInt64().ToString();
            var surface=form.world.Surfaces.Find(s=>s.Id==id);
            if (surface==null || !Fits(form,band,surface)) return;
            Native.SetWindowPos(Handle,IntPtr.Zero,raw.Left+inset.X,raw.Top+inset.Y,0,0,0x15);
        }
        protected override void Dispose(bool disposing) {
            if (disposing && strip != null) { strip.Dispose();strip=null; }
            base.Dispose(disposing);
        }
    }
}


