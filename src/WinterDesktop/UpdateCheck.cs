using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;

namespace WinterDesktop
{
    internal static class UpdateCheck
    {
        internal const string Latest = "https://github.com/kohb-git/Snowflake-on-Desktop/releases/latest";
        private static bool checking;

        internal static Version LatestVersion()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            var request = (HttpWebRequest)WebRequest.Create(Latest);
            request.Method = "HEAD";
            request.AllowAutoRedirect = false;
            request.Timeout = 8000;
            request.UserAgent = "snowlit-update-check";
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Uri target;
                Version version;
                string prefix = Latest.Replace("/latest", "/tag/v");
                if (!Uri.TryCreate(new Uri(Latest), response.Headers["Location"], out target) ||
                    !target.AbsoluteUri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !Version.TryParse(target.AbsoluteUri.Substring(prefix.Length), out version))
                    throw new FormatException("Не удалось прочитать версию выпуска.");
                return version;
            }
        }

        internal static void OpenLatest()
        {
            if (checking) return;
            checking = true;
            var worker = new BackgroundWorker();
            worker.DoWork += delegate(object sender, DoWorkEventArgs e) { e.Result = LatestVersion(); };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                checking = false;
                worker.Dispose();
                if (e.Error != null) {
                    MessageBox.Show("Не удалось проверить обновления. Попробуйте позже.", "snowlit");
                    return;
                }
                ShowResult((Version)e.Result);
            };
            worker.RunWorkerAsync();
        }

        private static void ShowResult(Version latest)
        {
            Version installed = new Version(typeof(UpdateCheck).Assembly.GetName().Version.ToString(3));
            if (latest <= installed) {
                MessageBox.Show("Установлена версия " + installed + ". Более новых выпусков нет.", "snowlit");
                return;
            }
            string message = "Доступна версия " + latest + ". Установлена " + installed + ". Открыть страницу скачивания?";
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
