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
	public sealed class SnowWindow : Form
	{
		private const int Transparent = 32;

		private const int ToolWindow = 128;

		private const int NoActivate = 134217728;

		private const int Layered = 524288;

		public readonly World world = new World(5721);

		private readonly Preferences preferences = Preferences.Load(Preferences.FilePath);

		private ToolStripMenuItem pauseItem;

		private ToolStripMenuItem exitItem;

		private Icon appIcon;

		private Native.WinEventProc locationCallback;

		public IntPtr locationHook;


		private System.Threading.Timer frameTimer;

		private int framePending;

		private volatile bool closing;

		public readonly Stopwatch clock = Stopwatch.StartNew();

		private readonly NotifyIcon tray = new NotifyIcon();

		private readonly uint processId = (uint)Process.GetCurrentProcess().Id;

		private LayerCanvas canvas;

		private readonly GarlandWindow garlandWindow = new GarlandWindow();
		public Rectangle desktop;

		private string screenLayout;

		private bool paused;

		private bool clockPeriod;

		private bool mousePassThrough = true;

		private double last;

		private PointF previous;

		internal int PresentedFrames;


		internal double RenderMilliseconds;

		private OptionsWindow options;

		private bool sessionLocked;

		private bool sleeping;

		private bool sessionRegistered;

		private int frameInterval = 16;

		private double lastPresent = -1.0;

		private int lastRenderedLayout = -1;

		private bool lastInactive;

		private readonly uint taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");

		public readonly Dictionary<IntPtr, Rectangle> frameInsets = new Dictionary<IntPtr, Rectangle>();

		public readonly Dictionary<IntPtr, Rectangle> lastNativeRects = new Dictionary<IntPtr, Rectangle>();

		private readonly Dictionary<uint, string> inputProcesses = new Dictionary<uint, string>();

		private bool Suspended
		{
			get
			{
				if (!sessionLocked)
				{
					return sleeping;
				}
				return true;
			}
		}

		private bool AllCovered
		{
			get
			{
				if (world.Screens.Count == 0)
				{
					return false;
				}
				foreach (ScreenArea screen in world.Screens)
				{
					bool flag = false;
					foreach (Rectangle fullScreenCover in world.FullScreenCovers)
					{
						if (fullScreenCover.Contains(screen.SnowArea))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						return false;
					}
				}
				return true;
			}
		}

		protected override bool ShowWithoutActivation => true;

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= 134742176;
				return createParams;
			}
		}

		private void ApplyPreferences()
		{
			world.GarlandEnabled = preferences.Garland;
			world.SetIntensity(preferences.Intensity);
			world.SetAccumulation(preferences.Ground, preferences.Windows);
			world.MeltSnow = preferences.Melt;
			world.NaturalSnow = preferences.Settle;
			world.GarlandTheme = preferences.Theme;
			world.LampScale = (float)preferences.LampSize / 100f;
			world.LightLevel = (float)preferences.Brightness / 100f;
			world.TwinkleSpeed = (float)preferences.Twinkle / 100f;
			world.SwayStrength = (float)preferences.Sway / 100f;
			if (preferences.Sway == 0)
			{
				foreach (Surface surface in world.Surfaces)
				{
					surface.GarlandSwing = (surface.GarlandVelocity = 0f);
					Array.Clear(surface.CordSway, 0, 16);
					Array.Clear(surface.CordSwaySpeed, 0, 16);
					Array.Clear(surface.CordSpeed, 0, 16);
				}
			}
			world.Release();
			base.Capture = false;
		}

		private void ShowScreenStatus()
		{
			RefreshWindows();
			StringBuilder stringBuilder = new StringBuilder("snowlit 1.0.8 · Проверка экранов\n\n");
			int num = 0;
			Screen[] allScreens = Screen.AllScreens;
			foreach (Screen screen in allScreens)
			{
				Rectangle workingArea = screen.WorkingArea;
				workingArea.Offset(-desktop.X, -desktop.Y);
				bool flag = false;
				foreach (Rectangle fullScreenCover in world.FullScreenCovers)
				{
					if (fullScreenCover.Contains(workingArea))
					{
						flag = true;
					}
				}
				stringBuilder.AppendLine("Экран " + ++num + (screen.Primary ? " · основной" : ""));
				stringBuilder.AppendLine(screen.Bounds.Width + " × " + screen.Bounds.Height + ", положение " + screen.Bounds.X + ", " + screen.Bounds.Y);
				stringBuilder.AppendLine(flag ? "Эффект скрыт активным полноэкранным окном." : "Эффект разрешён на этом экране.");
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("Снег: " + preferences.Intensity + "%. Гирлянда: " + (preferences.Garland ? "включена" : "выключена") + ".");
			stringBuilder.AppendLine("Настройки общие для всех экранов. На развёрнутых окнах гирлянда не размещается.");
			MessageBox.Show(stringBuilder.ToString(), "snowlit · Экраны", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}

		private void OpenOptions()
		{
			if (options != null && !options.IsDisposed)
			{
				options.Activate();
				return;
			}
			options = new OptionsWindow(preferences, delegate(Preferences next, bool startup)
			{
				Preferences preferences = this.preferences.Copy();
				if (!next.Save(Preferences.FilePath))
				{
					MessageBox.Show(options, "Не удалось сохранить настройки.", "snowlit");
					return false;
				}
				try
				{
					if (startup || RunAtLogin.Enabled)
					{
						RunAtLogin.Set(startup);
					}
				}
				catch (Exception ex)
				{
					if (!(ex is IOException) && !(ex is UnauthorizedAccessException) && !(ex is SecurityException))
					{
						throw;
					}
					preferences.Save(Preferences.FilePath);
					MessageBox.Show(options, "Не удалось изменить автозапуск: " + ex.Message, "snowlit");
					return false;
				}
				this.preferences.CopyFrom(next);
				ApplyPreferences();
				Render();
				return true;
			});
			options.Show();
		}

		public SnowWindow()
		{
			Text = "snowlit";
			base.FormBorderStyle = FormBorderStyle.None;
			base.ShowInTaskbar = false;
			base.StartPosition = FormStartPosition.Manual;
			base.TopMost = true;
			base.AutoScaleMode = AutoScaleMode.None;
			ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Items.Add("snowlit · 1.0.8").Enabled = false;
			pauseItem = new ToolStripMenuItem("Приостановить снег");
			ToolStripMenuItem toolStripMenuItem = pauseItem;
			EventHandler value = delegate
			{
				TogglePause();
			};
			toolStripMenuItem.Click += value;
			contextMenuStrip.Items.Add(pauseItem);
			contextMenuStrip.Items.Add("Настройки…", null, delegate
			{
				OpenOptions();
			});
			contextMenuStrip.Items.Add("Очистить снег и снеговиков", null, delegate
			{
				world.Clear();
				Render();
			});
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add("Проверить экраны…", null, delegate
			{
				ShowScreenStatus();
			});
			contextMenuStrip.Items.Add("Проверить обновления…", null, delegate
			{
				UpdateCheck.OpenLatest();
			});
			contextMenuStrip.Items.Add("Краткая справка", null, delegate
			{
				MessageBox.Show("Запуск: достаточно одного EXE. Установка и интернет не нужны.\n\nПравый щелчок по снежинке в трее — меню.\nДвойной щелчок — пауза; Ctrl+Shift+Q — выход, если сочетание свободно.\n\nВ настройках: снег, накопление, таяние, гирлянда и экономия ресурсов. Параметры общие для всех экранов.\nНа развёрнутых окнах гирлянды нет. Активное полноэкранное окно скрывает эффект на своём мониторе.\n\nДля обновления завершите старую версию через трей. Если включён автозапуск, нажмите «Применить» в настройках новой версии.\nНастройки сохраняются в %LOCALAPPDATA%\\QuietSnow. Перед удалением отключите автозапуск.", "snowlit · Справка", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			});
			contextMenuStrip.Items.Add("О приложении", null, delegate
			{
				MessageBox.Show("snowlit 1.0.8\nСнег и снеговики на рабочем столе.\n\nДвойной щелчок по значку — пауза.\nCtrl+Shift+Q — выход.\nНастройки сохраняются автоматически.\nРаботает без интернета и установки.", "snowlit", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			});
			exitItem = new ToolStripMenuItem("Выход · Ctrl+Shift+Q", null, delegate
			{
				Close();
			});
			contextMenuStrip.Items.Add(exitItem);
			appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
			tray.Icon = appIcon;
			tray.Text = "snowlit · снег идёт";
			tray.ContextMenuStrip = contextMenuStrip;
			tray.DoubleClick += delegate
			{
				TogglePause();
			};
			tray.Visible = true;
			ApplyPreferences();
			base.MouseDown += Press;
			base.MouseMove += Drag;
			base.MouseUp += Release;
			base.MouseCaptureChanged += delegate
			{
				if (!base.Capture)
				{
					world.Release();
				}
			};
			SetupScreens();
		}

		private void SavePreferences()
		{
			if (!preferences.Save(Preferences.FilePath))
			{
				tray.ShowBalloonTip(4000, "snowlit", "Не удалось сохранить настройки. Они действуют до выхода.", ToolTipIcon.Warning);
			}
		}

		private void TogglePause()
		{
			paused = !paused;
			world.MotionEffectsEnabled = !paused && !Suspended;
			world.Release();
			base.Capture = false;
			pauseItem.Text = (paused ? "Продолжить снег" : "Приостановить снег");
			pauseItem.Checked = paused;
			tray.Text = (paused ? "snowlit · пауза" : "snowlit · снег идёт");
			UpdateMouseAccess();
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			sessionRegistered = Native.WTSRegisterSessionNotification(base.Handle, 0);
			if (!Native.RegisterHotKey(base.Handle, 2, 16390u, 81u))
			{
				exitItem.Text = "Выход";
				tray.ShowBalloonTip(5000, "snowlit", "Ctrl+Shift+Q уже занято. Для выхода используйте меню значка в трее.", ToolTipIcon.Info);
			}
			bool moveQueued = false;
			locationCallback = delegate(IntPtr hook, uint evt, IntPtr window, int obj, int child, uint thread, uint time)
			{
				if (!closing && !Suspended && !paused && obj == 0 && child == 0 && lastNativeRects.ContainsKey(window))
				{
					garlandWindow.Follow(this, window);
					WindowTracking.Mark(window);
					if (!moveQueued)
					{
						moveQueued = true;
						BeginInvoke((Action)delegate
						{
							moveQueued = false;
							if (!closing && !Suspended && !paused) WindowTracking.MoveRefresh(this);
						});
					}
				}
			};
			locationHook = Native.SetWinEventHook(32779u, 32779u, IntPtr.Zero, locationCallback, 0u, 0u, 2u);
			RefreshWindows();
			Render();
			Native.HonorTimerResolution();
			clockPeriod = Native.timeBeginPeriod(1u) == 0;
			last = clock.Elapsed.TotalSeconds;
			frameTimer = new System.Threading.Timer(delegate
			{
				if (closing || Interlocked.Exchange(ref framePending, 1) != 0)
				{
					return;
				}
				try
				{
					BeginInvoke((Action)delegate
					{
						try
						{
							if (!closing)
							{
								Frame();
							}
						}
						finally
						{
							Interlocked.Exchange(ref framePending, 0);
						}
					});
				}
				catch (InvalidOperationException)
				{
					Interlocked.Exchange(ref framePending, 0);
				}
			}, null, 0, 16);
		}

		private void SetupScreens()
		{
			var list = MonitorLayout.Read();
			if (list.Count == 0) return;
			Rectangle virtualScreen = MonitorLayout.Normalize(list);
			string text = virtualScreen.ToString();
			foreach (var screen in list) text += "|" + screen.Id + screen.Bounds + screen.Work;
			if (!(text == screenLayout) || canvas == null)
			{
				LayerCanvas layerCanvas = new LayerCanvas(virtualScreen.Width, virtualScreen.Height);
				desktop = virtualScreen;
				base.Bounds = desktop;
				if (canvas != null)
				{
					canvas.Dispose();
				}
				canvas = layerCanvas;
				if (screenLayout != null)
				{
					world.Clear();
					world.Surfaces.Clear();
					world.Release();
					base.Capture = false;
					frameInsets.Clear();
					lastNativeRects.Clear();
				}
				screenLayout = text;
				world.SetScreens(list);
			}
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 786 && m.WParam.ToInt32() == 2)
			{
				Close();
				return;
			}
			if (taskbarCreated != 0 && m.Msg == taskbarCreated && !closing)
			{
				tray.Visible = false;
				tray.Visible = true;
			}
			if (m.Msg == 689)
			{
				if (m.WParam.ToInt32() == 7)
				{
					sessionLocked = true;
				}
				if (m.WParam.ToInt32() == 8)
				{
					sessionLocked = false;
				}
				last = clock.Elapsed.TotalSeconds;
				world.Release();
				base.Capture = false;
			}
			if (m.Msg == 536)
			{
				if (m.WParam.ToInt32() == 4)
				{
					sleeping = true;
					world.Release();
					base.Capture = false;
				}
				if (m.WParam.ToInt32() == 7 || m.WParam.ToInt32() == 18)
				{
					sleeping = false;
				}
				last = clock.Elapsed.TotalSeconds;
			}
			if (m.Msg == 33)
			{
				m.Result = new IntPtr(3);
				return;
			}
			if ((m.Msg == 126 || m.Msg == 736 || m.Msg == 26) && base.IsHandleCreated && !base.IsDisposed && !closing)
			{
				BeginInvoke((Action)delegate
				{
					if (!base.IsDisposed && !closing)
					{
						SetupScreens();
						RefreshWindows();
						Render();
					}
				});
			}
			base.WndProc(ref m);
		}

		private void UpdateMouseAccess()
		{
			Point position = Cursor.Position;
			position.Offset(-desktop.Left, -desktop.Top);
			if (base.Capture)
			{
				foreach (Rectangle fullScreenCover in world.FullScreenCovers)
				{
					if (fullScreenCover.Contains(position))
					{
						world.Release();
						base.Capture = false;
						break;
					}
				}
			}
			world.Pointer = position;
			world.PointerPresent = !paused && !Suspended && preferences.Interactive && world.InSnowArea(position);
			bool flag = paused || Suspended || !preferences.Interactive || (!base.Capture && !world.HitTest(position));
			if (flag != mousePassThrough)
			{
				mousePassThrough = flag;
				int windowLong = Native.GetWindowLong(base.Handle, -20);
				Native.SetWindowLong(base.Handle, -20, flag ? (windowLong | 0x20) : (windowLong & -33));
				Native.SetWindowPos(base.Handle, new IntPtr(-1), 0, 0, 0, 0, 51u);
				Cursor = (flag ? Cursors.Default : Cursors.Hand);
			}
		}

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
				bool shelf = CanDecorateWindow(text, Native.GetWindowLong(h, -16), windowLong, rectangle4);
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

		private void Frame()
		{
			double totalSeconds = clock.Elapsed.TotalSeconds;
			double num = Math.Min(1.0 / 30.0, totalSeconds - last);
			last = totalSeconds;
			bool onBattery = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline;
			int num2 = FramePolicy.Interval(preferences.Performance, onBattery, preferences.AutoEconomy, Suspended, AllCovered, paused);
			if (frameInterval != num2)
			{
				frameInterval = num2;
				if (frameTimer != null)
				{
					frameTimer.Change(num2, num2);
				}
				if (num2 >= 33 && clockPeriod)
				{
					Native.timeEndPeriod(1u);
					clockPeriod = false;
				}
				else if (num2 < 33 && !clockPeriod)
				{
					clockPeriod = Native.timeBeginPeriod(1u) == 0;
				}
			}
			world.MotionEffectsEnabled = !paused && !Suspended;
			if (Suspended)
			{
				UpdateMouseAccess();
				return;
			}
			if (lastNativeRects.ContainsKey(Native.GetForegroundWindow()))
			{
				WindowTracking.Mark(Native.GetForegroundWindow());
			}
			WindowTracking.FrameRefresh(this);
			UpdateMouseAccess();
			if (!paused && !AllCovered && num > 0.0)
			{
				int num3 = Math.Max(1, (int)Math.Ceiling(num * 120.0));
				float dt = (float)(num / (double)num3);
				for (int i = 0; i < num3; i++)
				{
					world.Step(dt);
				}
			}
			bool flag = paused || AllCovered;
			if (!flag || !lastInactive || lastRenderedLayout != world.LayoutVersion)
			{
				Render();
			}
			lastInactive = flag;
		}

		public void Render()
		{
			if (!closing && canvas != null && base.IsHandleCreated)
			{
				long timestamp = Stopwatch.GetTimestamp();
				var separate = garlandWindow.Prepare(this, (byte)(preferences.Opacity * 255 / 100));
				Painter.Draw(canvas.Graphics, world, separate);
				canvas.Present(base.Handle, desktop, (byte)(preferences.Opacity * 255 / 100));
				garlandWindow.FinishFrame(separate);
				lastPresent = clock.Elapsed.TotalSeconds;
				lastRenderedLayout = world.LayoutVersion;
				PresentedFrames++;
				RenderMilliseconds += (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
			}
		}

		private void Press(object sender, MouseEventArgs e)
		{
			if (!paused && !Suspended && preferences.Interactive && e.Button == MouseButtons.Left && world.HitTest(e.Location))
			{
				world.Pick(e.Location);
				previous = e.Location;
				base.Capture = true;
				Render();
			}
		}

		private void Drag(object sender, MouseEventArgs e)
		{
			if (world.Held != null)
			{
				float num = (float)e.X - previous.X;
				float num2 = (float)e.Y - previous.Y;
				float num3 = (float)Math.Sqrt(num * num + num2 * num2);
				int num4 = Math.Max(1, (int)Math.Ceiling(num3 / 4f));
				for (int i = 1; i <= num4; i++)
				{
					world.Drag(new PointF(previous.X + num * (float)i / (float)num4, previous.Y + num2 * (float)i / (float)num4), num3 / (float)num4);
				}
				previous = e.Location;
				Render();
			}
		}

		private void Release(object sender, MouseEventArgs e)
		{
			world.Release();
			base.Capture = false;
			Render();
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
		}

		protected override void OnPaint(PaintEventArgs e)
		{
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			closing = true;
			garlandWindow.Dispose();
			if (options != null)
			{
				options.Close();
			}
			if (sessionRegistered)
			{
				Native.WTSUnRegisterSessionNotification(base.Handle);
			}
			if (locationHook != IntPtr.Zero)
			{
				Native.UnhookWinEvent(locationHook);
			}
			if (frameTimer != null)
			{
				frameTimer.Dispose();
			}
			Native.UnregisterHotKey(base.Handle, 2);
			if (clockPeriod)
			{
				Native.timeEndPeriod(1u);
			}
			tray.Visible = false;
			if (tray.ContextMenuStrip != null)
			{
				tray.ContextMenuStrip.Dispose();
			}
			tray.Dispose();
			if (appIcon != null)
			{
				appIcon.Dispose();
			}
			if (canvas != null)
			{
				canvas.Dispose();
			}
			base.OnFormClosed(e);
		}
	}
}




