using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WinterDesktop
{
    internal static class MonitorLayout
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MonitorInfo
        {
            internal int Size;
            internal Native.RECT Bounds, Work;
            internal uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] internal string Device;
        }
        [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
        private delegate bool Visit(IntPtr monitor, IntPtr dc, IntPtr rectangle, IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, Visit visit, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        private static Rectangle Rect(Native.RECT r) { return Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom); }
        internal static List<ScreenArea> Read()
        {
            var screens = new List<ScreenArea>();
            bool ok = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr monitor, IntPtr dc, IntPtr rect, IntPtr data) {
                var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
                if (!GetMonitorInfo(monitor, ref info)) return false;
                var screen=new ScreenArea(info.Device, Rect(info.Bounds), Rect(info.Work));
                try { uint x,y; if(GetDpiForMonitor(monitor,0,out x,out y)==0) screen.Dpi=(int)x; }
                catch(DllNotFoundException){} catch(EntryPointNotFoundException){}
                screens.Add(screen);
                return true;
            }, IntPtr.Zero);
            if (!ok) screens.Clear();
            return screens;
        }
        internal static Rectangle Normalize(List<ScreenArea> screens)
        {
            Rectangle desktop = screens[0].Bounds;
            foreach (var s in screens) desktop = Rectangle.Union(desktop, s.Bounds);
            foreach (var s in screens) {
                s.Bounds.Offset(-desktop.X,-desktop.Y);
                s.Work.Offset(-desktop.X,-desktop.Y);
                s.SnowArea = Rectangle.Intersect(s.Bounds,s.Work);
            }
            return desktop;
        }
    }
}

