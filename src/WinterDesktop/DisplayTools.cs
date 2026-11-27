using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinterDesktop
{
    internal static class DisplayTools
    {
        private sealed class NumberWindow : Form
        {
            protected override bool ShowWithoutActivation { get { return true; } }
            protected override CreateParams CreateParams {
                get { var p=base.CreateParams; p.ExStyle |= 0x080000A0; return p; }
            }
        }
        internal static void Identify()
        {
            int number=0;
            foreach(var screen in MonitorLayout.Read()) {
                var f=new NumberWindow { StartPosition=FormStartPosition.Manual, FormBorderStyle=FormBorderStyle.None,
                    ShowInTaskbar=false, TopMost=true, Bounds=new Rectangle(screen.Work.Left+24,screen.Work.Top+24,120,90), BackColor=Color.MidnightBlue };
                f.Controls.Add(new Label { Dock=DockStyle.Fill,Text=(++number).ToString(),ForeColor=Color.White,
                    Font=new Font(SystemFonts.MessageBoxFont.FontFamily,36), TextAlign=ContentAlignment.MiddleCenter });
                var timer=new Timer {Interval=2200};
                timer.Tick+=delegate { f.Close(); };
                f.FormClosed+=delegate {timer.Dispose();};
                f.Show();timer.Start();
            }
        }
        internal static void Report(string text)
        {
            using(var f=new Form {Text="snowlit · Экраны", ClientSize=new Size(660,400),StartPosition=FormStartPosition.CenterScreen})
            using(var box=new TextBox {Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=text})
            using(var copy=new Button {Dock=DockStyle.Bottom,Text="Скопировать отчёт",Height=38}) {
                copy.Click+=delegate {try { Clipboard.SetText(text); copy.Text="Скопировано"; }
                    catch(System.Runtime.InteropServices.ExternalException){box.Focus();box.SelectAll();MessageBox.Show("Буфер обмена занят. Повторите копирование.");}};
                f.Controls.Add(box);f.Controls.Add(copy);f.ShowDialog();
            }
        }
    }
}
