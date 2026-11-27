using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WinterDesktop
{
    internal static class UpdateInstaller
    {
        private const string Repo = "kohb-git/Snowflake-on-Desktop";
        private const int MaxSize = 32 * 1024 * 1024;
        public sealed class Release { public string tag_name { get; set; } public Asset[] assets { get; set; } }
        public sealed class Asset {
            public string name { get; set; } public string browser_download_url { get; set; }
            public string digest { get; set; } public long size { get; set; }
        }
        internal static readonly string Cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuietSnow","updates");
        private static HttpWebRequest Request(string url)
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            var request=(HttpWebRequest)WebRequest.Create(url);
            request.UserAgent="snowlit-updater"; request.Timeout=15000; request.ReadWriteTimeout=15000;
            return request;
        }
        internal static Asset Find(Version version)
        {
            using(var response=Request("https://api.github.com/repos/"+Repo+"/releases/latest").GetResponse())
            using(var reader=new StreamReader(response.GetResponseStream())) {
                var release=new JavaScriptSerializer().Deserialize<Release>(reader.ReadToEnd());
                string name="snowlit-"+version+".exe";
                if(release!=null && release.tag_name=="v"+version && release.assets!=null)
                    foreach(var asset in release.assets)
                        if(asset.name==name && asset.size>0 && asset.size<=MaxSize &&
                           asset.browser_download_url=="https://github.com/"+Repo+"/releases/download/v"+version+"/"+name &&
                           System.Text.RegularExpressions.Regex.IsMatch(asset.digest??"", @"\Asha256:[0-9a-f]{64}\z"))
                            return asset;
                throw new InvalidDataException("Выпуск изменился или не содержит проверяемого EXE. Проверьте обновления ещё раз.");
            }
        }
        internal static string Stage(Version version)
        {
            var asset=Find(version);
            Directory.CreateDirectory(Cache);
            string path=Path.Combine(Cache,"update-"+Guid.NewGuid().ToString("N")+".exe");
            try {
                using(var response=Request(asset.browser_download_url).GetResponse())
                using(var input=response.GetResponseStream())
                using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    byte[] buffer=new byte[65536]; int n;
                    while((n=input.Read(buffer,0,buffer.Length))>0) {
                        if(output.Length+n>asset.size) throw new InvalidDataException("Превышен размер файла.");
                        output.Write(buffer,0,n);
                    }
                    if(output.Length!=asset.size) throw new EndOfStreamException("Загрузка неполная.");
                }
                Verify(path,asset.digest,version);
                return path;
            } catch { File.Delete(path); throw; }
        }
        internal static void Verify(string path,string expected,Version version)
        {
            using(var input=File.OpenRead(path))
            using(var sha=SHA256.Create()) {
                string digest="sha256:"+BitConverter.ToString(sha.ComputeHash(input)).Replace("-","").ToLowerInvariant();
                if(digest!=expected) throw new InvalidDataException("Контрольная сумма EXE не совпала.");
            }
            if(FileVersionInfo.GetVersionInfo(path).ProductName!="snowlit" ||
               AssemblyName.GetAssemblyName(path).Version.ToString(3)!=version.ToString())
                throw new InvalidDataException("Загруженный EXE не соответствует выпуску.");
        }
        internal static void CleanOldDownloads()
        {
            try {
                if(Directory.Exists(Cache)) foreach(string f in Directory.GetFiles(Cache,"update-*.exe"))
                    if(File.GetLastWriteTimeUtc(f)<DateTime.UtcNow.AddDays(-1)) File.Delete(f);
            } catch(IOException){} catch(UnauthorizedAccessException){}
        }
        internal static void Launch(string file,string args)
        {
            using(Process.Start(new ProcessStartInfo(file,args){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(file)})){}
        }
        internal static void Finish(string target,string parentId)
        {
            string stage=null, backup=null;
            try {
                target=Path.GetFullPath(target);
                if(!string.Equals(Path.GetDirectoryName(Application.ExecutablePath),Cache,StringComparison.OrdinalIgnoreCase) ||
                   !string.Equals(Path.GetExtension(target),".exe",StringComparison.OrdinalIgnoreCase) ||
                   FileVersionInfo.GetVersionInfo(target).ProductName!="snowlit" ||
                   AssemblyName.GetAssemblyName(target).Version>=Assembly.GetExecutingAssembly().GetName().Version)
                    throw new InvalidDataException("Файл назначения не является предыдущей версией snowlit.");
                int pid=int.Parse(parentId);
                Process parent=null;
                try {parent=Process.GetProcessById(pid);} catch(ArgumentException){}
                using(parent) {
                    if(parent!=null && !parent.WaitForExit(30000)) throw new IOException("Предыдущая версия ещё работает.");
                }
                string suffix=Guid.NewGuid().ToString("N");
                stage=target+"."+suffix+".tmp";backup=target+"."+suffix+".bak";
                File.Copy(Application.ExecutablePath,stage);
                File.Replace(stage,target,backup);
                try { Launch(target,""); }
                catch { File.Replace(backup,target,null); Launch(target,""); throw; }
                try {File.Delete(backup);} catch(IOException){} catch(UnauthorizedAccessException){}
            } catch(Exception ex) {
                MessageBox.Show("Обновление не завершено. Файл приложения: "+target+"\n"+ex.Message,"snowlit");
            } finally {
                try {if(stage!=null) File.Delete(stage);}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }
    }
}
