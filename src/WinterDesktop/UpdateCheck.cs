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

        internal static void OpenLatest(Form owner)
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
                if (!owner.IsDisposed) ShowResult((Version)e.Result, owner);
            };
            worker.RunWorkerAsync();
        }

        private static void ShowResult(Version latest, Form owner)
        {
            Version installed = new Version(typeof(UpdateCheck).Assembly.GetName().Version.ToString(3));
            if (latest <= installed) {
                MessageBox.Show("Установлена версия " + installed + ". Более новых выпусков нет.", "snowlit");
                return;
            }
            string message = "Доступна версия " + latest + ". Установлена " + installed + ". Скачать, проверить и установить обновление? После установки snowlit перезапустится.";
            if (MessageBox.Show(message, "snowlit · Проверить обновления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
            checking=true;
            var download=new BackgroundWorker();
            download.DoWork+=delegate(object sender,DoWorkEventArgs e) {
                string probe=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath),".snowlit-"+Guid.NewGuid().ToString("N")+".tmp");
                using(new System.IO.FileStream(probe,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.None,4096,System.IO.FileOptions.DeleteOnClose)){}
                e.Result=UpdateInstaller.Stage(latest);
            };
            download.RunWorkerCompleted+=delegate(object sender,RunWorkerCompletedEventArgs e) {
                checking=false; download.Dispose();
                if(owner.IsDisposed)return;
                if(e.Error!=null){MessageBox.Show("Не удалось установить обновление. "+e.Error.Message,"snowlit");return;}
                try {
                    UpdateInstaller.Launch((string)e.Result,"--finish-update \""+Application.ExecutablePath+"\" "+Process.GetCurrentProcess().Id);
                    owner.Close();
                } catch(Win32Exception ex){MessageBox.Show(ex.Message,"snowlit");}
            };
            download.RunWorkerAsync();
        }
    }
}
