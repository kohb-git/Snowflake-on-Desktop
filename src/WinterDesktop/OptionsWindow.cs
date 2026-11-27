using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinterDesktop
{
	internal sealed class OptionsWindow : Form
	{
		private readonly Preferences draft;

		private readonly Func<Preferences, bool, bool> commit;

		private readonly CheckBox startup = new CheckBox();

		internal OptionsWindow(Preferences current, Func<Preferences, bool, bool> save)
		{
			draft = current.Copy();
			commit = save;
			Text = "snowlit · Настройки";
			Font = new Font("Segoe UI", 10f);
			base.AutoScaleMode = AutoScaleMode.Dpi;
			base.ClientSize = new Size(570, 600);
			MinimumSize = new Size(570, 600);
			base.StartPosition = FormStartPosition.CenterScreen;
			TabControl tabControl = new TabControl
			{
				Dock = DockStyle.Fill
			};
			base.Controls.Add(tabControl);
			FlowLayoutPanel p = Page(tabControl, "Снег");
			FlowLayoutPanel p2 = Page(tabControl, "Гирлянда");
			FlowLayoutPanel flowLayoutPanel = Page(tabControl, "Работа");
			var screens = Page(tabControl,"Экраны");
			var identify = new Button { Text="Определить экраны", AutoSize=true };
			identify.Click += delegate { DisplayTools.Identify(); };
			screens.Controls.Add(identify);
			int number=0;
			foreach(var screen in MonitorLayout.Read()) {
				string id=screen.Id;
				Choice(screens,"Экран " + ++number + " · " + id,new[]{"Общие настройки","Снег и гирлянда","Только снег","Только гирлянда","Выключено"},draft.Mode(id),delegate(int mode){
					if(mode==0) draft.Modes.Remove(id); else draft.Modes[id]=mode;
				});
			}
			var preview = new Panel { Width=470, Height=76, BackColor=Color.FromArgb(34,42,53) };
			var sample=new Surface("preview",new Rectangle(12,8,444,60),false);
			preview.Paint += delegate(object sender, PaintEventArgs args) {
				var model=new World(1){Display=draft,GarlandTheme=draft.Theme,LampScale=draft.LampSize/100f,LightLevel=draft.Brightness/100f};
				args.Graphics.DrawLine(Pens.SlateGray,12,8,456,8);
				if(draft.Garland) Painter.DrawGarland(args.Graphics,sample,0,model);
			};
			p2.Controls.Add(preview);
			Slider(p2,"Отступ от верхнего края",0,16,draft.CordOffset,delegate(int v){draft.CordOffset=v;preview.Invalidate();}," пикс.");
			Slider(p2,"Провисание",0,100,draft.SagPercent,delegate(int v){draft.SagPercent=v;preview.Invalidate();});
			int intensity = draft.Intensity;
			Action<int> set = delegate(int v)
			{
				draft.Intensity = v;
			};
			Slider(p, "Интенсивность снегопада", 0, 200, intensity, set);
			Slider(p, "Видимость всего эффекта", 10, 100, draft.Opacity, delegate(int v)
			{
				draft.Opacity = v;
			});
			Toggle(p, "Накапливать снег на окнах", draft.Windows, delegate(bool b)
			{
				draft.Windows = b;
			});
			Toggle(p, "Накапливать снег внизу экрана", draft.Ground, delegate(bool b)
			{
				draft.Ground = b;
			});
			Toggle(p, "Реагировать на мышь", draft.Interactive, delegate(bool b)
			{
				draft.Interactive = b;
			});
			Toggle(p, "Мягкое оседание крутых сугробов", draft.Settle, delegate(bool b)
			{
				draft.Settle = b;
			});
			Toggle(p, "Постепенное таяние сугробов", draft.Melt, delegate(bool b)
			{
				draft.Melt = b;
			});
			Note(p, "Отключение накопления убирает сугробы с выбранных поверхностей. Снеговики остаются. При интенсивности 0 новые снежинки не появляются.");
			Toggle(p2, "Показывать гирлянду", draft.Garland, delegate(bool b)
			{
				draft.Garland = b; preview.Invalidate();
			});
			Choice(p2, "Цветовая тема", new string[3] { "Разноцветная", "Тёплый белый", "Ледяная" }, draft.Theme, delegate(int v)
			{
				draft.Theme = v; preview.Invalidate();
			});
			Slider(p2, "Размер лампочек", 60, 150, draft.LampSize, delegate(int v)
			{
				draft.LampSize = v; preview.Invalidate();
			});
			Slider(p2, "Яркость", 10, 100, draft.Brightness, delegate(int v)
			{
				draft.Brightness = v; preview.Invalidate();
			});
			Slider(p2, "Скорость мерцания · 0 — постоянный свет", 0, 200, draft.Twinkle, delegate(int v)
			{
				draft.Twinkle = v;
			});
			Slider(p2, "Сила раскачивания", 0, 200, draft.Sway, delegate(int v)
			{
				draft.Sway = v;
			});
			Choice(flowLayoutPanel, "Плавность и нагрузка", new string[3] { "Сбалансированный · до 60 кадров/с", "Экономичный · до 30 кадров/с", "Плавный · до 120 кадров/с" }, draft.Performance, delegate(int v)
			{
				draft.Performance = v;
			});
			Toggle(flowLayoutPanel, "Экономить заряд при работе от батареи", draft.AutoEconomy, delegate(bool b)
			{
				draft.AutoEconomy = b;
			});
			startup.Text = "Запускать при входе в Windows";
			startup.AutoSize = true;
			startup.Checked = RunAtLogin.Enabled;
			flowLayoutPanel.Controls.Add(startup);
			Note(flowLayoutPanel, "Автозапуск использует файл из текущей папки. Если переместите приложение, выключите и снова включите эту настройку.\n\nВо время сна и блокировки анимация приостанавливается. Полноэкранные приложения скрывают эффект на своём мониторе. Реальная частота кадров зависит от нагрузки.");
			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 55,
				FlowDirection = FlowDirection.RightToLeft,
				Padding = new Padding(8)
			};
			Button button = new Button
			{
				Text = "Применить",
				AutoSize = true
			};
			Button button2 = new Button
			{
				Text = "Отмена",
				AutoSize = true
			};
			button.Click += delegate
			{
				if (commit(draft, startup.Checked))
				{
					Close();
				}
			};
			button2.Click += delegate
			{
				Close();
			};
			flowLayoutPanel2.Controls.Add(button);
			flowLayoutPanel2.Controls.Add(button2);
			base.Controls.Add(flowLayoutPanel2);
			tabControl.BringToFront();
			base.AcceptButton = button;
			base.CancelButton = button2;
		}

		private static FlowLayoutPanel Page(TabControl tabs, string name)
		{
			TabPage tabPage = new TabPage(name);
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
			flowLayoutPanel.Dock = DockStyle.Fill;
			flowLayoutPanel.AutoScroll = true;
			flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
			flowLayoutPanel.WrapContents = false;
			flowLayoutPanel.Padding = new Padding(16);
			FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
			tabPage.Controls.Add(flowLayoutPanel2);
			tabs.TabPages.Add(tabPage);
			return flowLayoutPanel2;
		}

		private static void Note(FlowLayoutPanel p, string text)
		{
			p.Controls.Add(new Label
			{
				Text = text,
				AutoSize = true,
				MaximumSize = new Size(490, 0),
				Margin = new Padding(0, 12, 0, 10)
			});
		}

		private static void Toggle(FlowLayoutPanel p, string text, bool value, Action<bool> set)
		{
			CheckBox c = new CheckBox
			{
				Text = text,
				Checked = value,
				AutoSize = true,
				Margin = new Padding(0, 6, 0, 6)
			};
			c.CheckedChanged += delegate
			{
				set(c.Checked);
			};
			p.Controls.Add(c);
		}

		private static void Choice(FlowLayoutPanel p, string title, string[] items, int value, Action<int> set)
		{
			Note(p, title);
			ComboBox c = new ComboBox
			{
				Width = 470,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			c.Items.AddRange(items);
			c.SelectedIndex = value;
			c.SelectedIndexChanged += delegate
			{
				set(c.SelectedIndex);
			};
			p.Controls.Add(c);
		}

		private static void Slider(FlowLayoutPanel p, string title, int min, int max, int value, Action<int> set, string unit = "%")
		{
			Label label = new Label
			{
				AutoSize = true,
				Text = title + ": " + value + unit,
				Margin = new Padding(0, 8, 0, 0)
			};
			p.Controls.Add(label);
			TrackBar slider = new TrackBar
			{
				Minimum = min,
				Maximum = max,
				Value = Math.Max(min, Math.Min(max, value)),
				Width = 480,
				Height = 38,
				TickStyle = TickStyle.None,
				SmallChange = 1,
				LargeChange = 10
			};
			slider.ValueChanged += delegate
			{
				label.Text = title + ": " + slider.Value + unit;
				set(slider.Value);
			};
			p.Controls.Add(slider);
		}
	}
}

