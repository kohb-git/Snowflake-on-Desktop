using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinterDesktop
{
	internal static class RunAtLogin
	{
		private const string Key = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

		internal static bool Enabled
		{
			get
			{
				try
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
					{
						return registryKey != null && registryKey.GetValue("QuietSnow") != null;
					}
				}
				catch (SecurityException)
				{
					return false;
				}
			}
		}

		internal static void Set(bool enabled)
		{
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
			{
				if (enabled)
				{
					registryKey.SetValue("QuietSnow", "\"" + Application.ExecutablePath + "\"");
				}
				else
				{
					registryKey.DeleteValue("QuietSnow", throwOnMissingValue: false);
				}
			}
		}
	}
}
