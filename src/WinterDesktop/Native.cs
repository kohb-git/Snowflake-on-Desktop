using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WinterDesktop
{
	public static class Native
	{
		public struct RECT
		{
			public int Left;

			public int Top;

			public int Right;

			public int Bottom;
		}

		internal struct POINT
		{
			public int X;

			public int Y;

			public POINT(int x, int y)
			{
				X = x;
				Y = y;
			}
		}

		internal struct SIZE
		{
			public int X;

			public int Y;

			public SIZE(int x, int y)
			{
				X = x;
				Y = y;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct BLEND
		{
			public byte Op;

			public byte Flags;

			public byte Alpha;

			public byte Format;
		}

		internal struct POWER_THROTTLING
		{
			public uint Version;

			public uint ControlMask;

			public uint StateMask;
		}

		internal struct BITMAPINFO
		{
			public uint Size;

			public int Width;

			public int Height;

			public ushort Planes;

			public ushort BitCount;

			public uint Compression;

			public uint ImageSize;

			public int XPels;

			public int YPels;

			public uint Used;

			public uint Important;
		}

		internal delegate void WinEventProc(IntPtr hook, uint evt, IntPtr window, int obj, int child, uint thread, uint time);

		internal delegate bool EnumProc(IntPtr h, IntPtr p);

		[DllImport("wtsapi32.dll")]
		internal static extern bool WTSRegisterSessionNotification(IntPtr h, int flags);

		[DllImport("wtsapi32.dll")]
		internal static extern bool WTSUnRegisterSessionNotification(IntPtr h);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		internal static extern uint RegisterWindowMessage(string name);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		internal static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventProc callback, uint process, uint thread, uint flags);

		[DllImport("user32.dll")]
		internal static extern bool UnhookWinEvent(IntPtr hook);

		[DllImport("user32.dll")]
		internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);

		[DllImport("user32.dll")]
		internal static extern bool SetProcessDPIAware();

		[DllImport("user32.dll")]
		internal static extern bool EnumWindows(EnumProc cb, IntPtr p);

		[DllImport("user32.dll")]
		public static extern bool IsWindowVisible(IntPtr h);

		[DllImport("user32.dll")]
		public static extern bool IsIconic(IntPtr h);

		[DllImport("user32.dll")]
		public static extern bool IsZoomed(IntPtr h);

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr h, out RECT r);

		[DllImport("user32.dll")]
		internal static extern uint GetWindowThreadProcessId(IntPtr h, out uint process);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		internal static extern int GetClassName(IntPtr h, StringBuilder name, int max);

		[DllImport("user32.dll")]
		internal static extern int GetWindowLong(IntPtr h, int index);

		[DllImport("user32.dll")]
		internal static extern int SetWindowLong(IntPtr h, int index, int value);

		[DllImport("user32.dll")]
		internal static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint key);

		[DllImport("user32.dll")]
		internal static extern bool UnregisterHotKey(IntPtr h, int id);

		[DllImport("user32.dll")]
		internal static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int ht, uint flags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool UpdateLayeredWindow(IntPtr h, IntPtr dst, ref POINT pos, ref SIZE size, IntPtr src, ref POINT zero, uint key, ref BLEND blend, uint flags);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr CreateCompatibleDC(IntPtr dc);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFO info, uint usage, out IntPtr bits, IntPtr section, uint offset);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

		[DllImport("gdi32.dll")]
		internal static extern bool DeleteObject(IntPtr obj);

		[DllImport("gdi32.dll")]
		internal static extern bool DeleteDC(IntPtr dc);

		[DllImport("dwmapi.dll")]
		internal static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT r, int size);

		[DllImport("dwmapi.dll")]
		internal static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int value, int size);

		[DllImport("winmm.dll")]
		internal static extern uint timeBeginPeriod(uint period);

		[DllImport("winmm.dll")]
		internal static extern uint timeEndPeriod(uint period);

		[DllImport("kernel32.dll")]
		private static extern bool SetProcessInformation(IntPtr process, int kind, ref POWER_THROTTLING state, uint size);

		internal static void HonorTimerResolution()
		{
			try
			{
				POWER_THROTTLING pOWER_THROTTLING = default(POWER_THROTTLING);
				pOWER_THROTTLING.Version = 1u;
				pOWER_THROTTLING.ControlMask = 4u;
				pOWER_THROTTLING.StateMask = 0u;
				POWER_THROTTLING state = pOWER_THROTTLING;
				SetProcessInformation(new IntPtr(-1), 4, ref state, 12u);
			}
			catch (EntryPointNotFoundException)
			{
			}
		}
	}
}
