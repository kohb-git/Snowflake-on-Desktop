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
	public sealed partial class SnowWindow : Form
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
			world.Display = preferences;
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
			var report=new StringBuilder("snowlit 1.2.0 · Диагностика экранов");
			report.AppendLine().AppendLine();
			int n=0;
			foreach (var screen in world.Screens) {
				var bounds=screen.Bounds; bounds.Offset(desktop.Location);
				var work=screen.SnowArea; work.Offset(desktop.Location);
				report.AppendLine("Экран " + ++n + " · " + screen.Id);
				report.AppendLine("Границы: " + bounds + ". Рабочая область эффекта: " + work);
				report.AppendLine("Масштаб: " + (screen.Dpi>0 ? Math.Round(screen.Dpi*100.0/96)+"%" : "не определён"));
				report.AppendLine("Снег: " + (preferences.SnowOn(screen.Id) ? preferences.Intensity+"%" : "выключен") +
					". Гирлянда: " + (preferences.LightsOn(screen.Id) ? "включена" : "выключена"));
				report.AppendLine(pauseUntil.HasValue ? "Эффекты скрыты до " + pauseUntil.Value.ToLocalTime().ToShortTimeString() :
					paused ? "Анимация приостановлена." : Suspended ? "Сеанс заблокирован или компьютер спит." :
					world.FullScreenCovers.Exists(r=>r.Contains(screen.SnowArea)) ? "Активное полноэкранное окно скрывает эффекты." :
					"Снег виден на свободном рабочем столе. Гирлянда видна на подходящих неразвёрнутых окнах.");
				report.AppendLine();
			}
			DisplayTools.Report(report.ToString());
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
			contextMenuStrip.Items.Add("snowlit · 1.2.0").Enabled = false;
			pauseItem = new ToolStripMenuItem("Приостановить снег");
			ToolStripMenuItem toolStripMenuItem = pauseItem;
			EventHandler value = delegate
			{
				TogglePause();
			};
			toolStripMenuItem.Click += value;
			contextMenuStrip.Items.Add(pauseItem);
			var timed = new ToolStripMenuItem("Пауза на время");
			timed.DropDownItems.Add("На 30 минут",null,delegate { PauseFor(30); });
			timed.DropDownItems.Add("На 2 часа",null,delegate { PauseFor(120); });
			contextMenuStrip.Items.Add(timed);
			contextMenuStrip.Items.Add("Настройки…", null, delegate
			{
				BeginInvoke((Action)delegate { if (!closing) OpenOptions(); });
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
				UpdateCheck.OpenLatest(this);
			});
			contextMenuStrip.Items.Add("Краткая справка", null, delegate
			{
				MessageBox.Show("Запуск: достаточно одного EXE. Установка и интернет не нужны.\n\nПравый щелчок по снежинке в трее — меню.\nДвойной щелчок — пауза; Ctrl+Shift+Q — выход, если сочетание свободно.\n\nВ настройках: снег, накопление, таяние, гирлянда и экономия ресурсов. Режим эффектов задаётся на вкладке «Экраны».\nНа развёрнутых окнах гирлянды нет. Активное полноэкранное окно скрывает эффект на своём мониторе.\n\nДля обновления завершите старую версию через трей. Если включён автозапуск, нажмите «Применить» в настройках новой версии.\nНастройки сохраняются в %LOCALAPPDATA%\\QuietSnow. Перед удалением отключите автозапуск.", "snowlit · Справка", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			});
			contextMenuStrip.Items.Add("О приложении", null, delegate
			{
				MessageBox.Show("snowlit 1.2.0\nСнег и снеговики на рабочем столе.\n\nДвойной щелчок по значку — пауза.\nCtrl+Shift+Q — выход.\nНастройки сохраняются автоматически.\nРаботает без интернета и установки.", "snowlit", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
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

		private DateTime? pauseUntil;
		private void PauseFor(int minutes)
		{
			if (!paused) TogglePause();
			pauseUntil=DateTime.UtcNow.AddMinutes(minutes);
			pauseItem.Text="Продолжить сейчас (пауза до " + pauseUntil.Value.ToLocalTime().ToShortTimeString() + ")";
			Render();
		}
		private void TogglePause()
		{
			pauseUntil=null;
			lastInactive=false;
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
				exitItem.ToolTipText = "Ctrl+Shift+Q занято другим приложением.";
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
			bool flag = paused || Suspended || !preferences.Interactive || (!base.Capture && (!world.Screens.Exists(s=>s.SnowArea.Contains(position) && preferences.SnowOn(s.Id)) || !world.HitTest(position)));
			if (flag != mousePassThrough)
			{
				mousePassThrough = flag;
				int windowLong = Native.GetWindowLong(base.Handle, -20);
				Native.SetWindowLong(base.Handle, -20, flag ? (windowLong | 0x20) : (windowLong & -33));
				Native.SetWindowPos(base.Handle, new IntPtr(-1), 0, 0, 0, 0, 51u);
				Cursor = (flag ? Cursors.Default : Cursors.Hand);
			}
		}

		private void Frame()
		{
			if (pauseUntil.HasValue && DateTime.UtcNow >= pauseUntil.Value) TogglePause();
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
			var foreground = Native.GetForegroundWindow();
			if (lastNativeRects.ContainsKey(foreground))
			{
				WindowTracking.Mark(foreground);
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
				if (pauseUntil.HasValue) {
					garlandWindow.FinishFrame(null);
					canvas.Present(Handle,desktop,0);
					return;
				}
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









