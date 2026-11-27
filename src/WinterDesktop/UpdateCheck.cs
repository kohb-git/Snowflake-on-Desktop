using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace WinterDesktop
{
    internal static class UpdateCheck
    {
        // The browser can use the user's existing GitHub session for private releases.
        internal const string Latest = "https://github.com/kohb-git/Snowflake-on-Desktop/releases/latest";

        internal static void OpenLatest()
        {
            Version installed = typeof(UpdateCheck).Assembly.GetName().Version;
            string message = "Установлена версия " + installed.ToString(3) +
                ". Открыть последний выпуск на GitHub? Если доступна более новая версия, скачайте EXE в разделе Assets.";
            if (MessageBox.Show(message, "snowlit · Проверить обновления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
            try
            {
                Process.Start(new ProcessStartInfo(Latest) { UseShellExecute = true });
            }
            catch (Win32Exception)
            {
                MessageBox.Show("Не удалось открыть браузер. Адрес последнего выпуска:\n" + Latest,
                    "snowlit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
