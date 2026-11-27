using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WinterDesktop
{
	internal static class Program
	{
		[STAThread]
		private static void Main()
		{
			bool createdNew;
			using (new Mutex(initiallyOwned: true, "Local\\WinterDesktop.SoftSnow.v2", out createdNew))
			{
				if (!createdNew)
				{
					return;
				}
				Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
				try
				{
					try
					{
						Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
					}
					catch (EntryPointNotFoundException)
					{
						Native.SetProcessDPIAware();
					}
					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(defaultValue: false);
					Application.Run(new SnowWindow());
				}
				catch (Exception ex2)
				{
					try
					{
						string directoryName = Path.GetDirectoryName(Preferences.FilePath);
						Directory.CreateDirectory(directoryName);
						File.WriteAllText(Path.Combine(directoryName, "error.log"), string.Concat(DateTime.Now, Environment.NewLine, ex2));
					}
					catch (IOException)
					{
					}
					catch (UnauthorizedAccessException)
					{
					}
					MessageBox.Show("Не удалось продолжить работу. Перезапустите приложение.\nЕсли журнал удалось сохранить, он находится в %LOCALAPPDATA%\\QuietSnow\\error.log.", "Тихий снег", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}
	}
}
