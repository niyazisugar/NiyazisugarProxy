using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

class UpdateRelease {
    public Version Version;
    public string Url, Digest;
}
static class Updates {
    const string Repo="niyazisugar/NiyazisugarProxy";
    public static Version Current {get {return Assembly.GetExecutingAssembly().GetName().Version;}}
    public static string Quote(string text) {
        if(text.IndexOf('"')>=0)throw new ArgumentException("Geçersiz dosya yolu.");
        return "\""+text+"\"";
    }
    static WebClient Client() {
        ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
        WebClient c=new WebClient();c.Headers["User-Agent"]="NiyazisugarProxy/"+Current;c.Headers["Accept"]="application/vnd.github+json";return c;
    }
    public static UpdateRelease Check() {
        string data;
        using(WebClient c=Client()) {
            try {data=c.DownloadString("https://api.github.com/repos/"+Repo+"/releases/latest");}
            catch(WebException e) {var response=e.Response as HttpWebResponse;if(response!=null && response.StatusCode==HttpStatusCode.NotFound)return null;throw;}
        }
        return Parse(data,Current);
    }
    public static UpdateRelease Parse(string data,Version current) {
        var d=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(data);
        if((bool)d["draft"] || (bool)d["prerelease"])return null;
        string tag=(string)d["tag_name"];Version v;
        if(!Version.TryParse(tag.TrimStart('v'),out v))throw new Exception("Sürüm numarası geçersiz.");
        // Compare in three components; assembly uses four.
        if(new Version(v.Major,v.Minor,Math.Max(0,v.Build),Math.Max(0,v.Revision))<=current)return null;
        foreach(object item in (IEnumerable)d["assets"]) {
            var a=(Dictionary<string,object>)item;
            if((string)a["name"]!="NiyazisugarProxy.exe")continue;
            string url=(string)a["browser_download_url"];Uri u=new Uri(url);
            if(u.Scheme!="https" || u.Host!="github.com" || !u.AbsolutePath.StartsWith("/"+Repo+"/releases/download/",StringComparison.Ordinal))throw new Exception("Güncelleme adresi doğrulanamadı.");
            string digest=a.ContainsKey("digest")?a["digest"] as string:null;
            if(digest==null || !System.Text.RegularExpressions.Regex.IsMatch(digest,"^sha256:[a-fA-F0-9]{64}$"))throw new Exception("Güncellemenin SHA256 bilgisi yok. Kurulum yapılmadı.");
            return new UpdateRelease {Version=v,Url=url,Digest=digest.Substring(7)};
        }
        throw new Exception("Sürümde NiyazisugarProxy.exe bulunamadı.");
    }
    public static string Hash(string file) {
        using(var f=File.OpenRead(file))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();
    }
    public static string Stage(UpdateRelease r) {
        string dir=Path.Combine(Path.GetTempPath(),"NiyazisugarProxy-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        string exe=Path.Combine(dir,"NiyazisugarProxy.exe");
        try {
            using(WebClient c=Client())c.DownloadFile(r.Url,exe);
            if(new FileInfo(exe).Length>50*1024*1024)throw new Exception("Güncelleme boyutu geçersiz.");
            if(!String.Equals(Hash(exe),r.Digest,StringComparison.OrdinalIgnoreCase))throw new Exception("Güncelleme dosyasının SHA256 kontrolü başarısız.");
            Version found=AssemblyName.GetAssemblyName(exe).Version;
            if(found.Major!=r.Version.Major||found.Minor!=r.Version.Minor||found.Build!=r.Version.Build)throw new Exception("Dosya sürümü uyuşmuyor.");
            return exe;
        }catch {Directory.Delete(dir,true);throw;}
    }
    public static void RunInstaller(string staged,string digest) {
        string helper=Path.Combine(Path.GetDirectoryName(staged),"installer.exe");
        File.Copy(Application.ExecutablePath,helper);
        string destination=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"NiyazisugarProxy.exe");
        Process.Start(new ProcessStartInfo {FileName=helper,Arguments="--apply-update "+Process.GetCurrentProcess().Id+" "+Quote(staged)+" "+Quote(destination)+" "+digest,UseShellExecute=true});
    }
    public static void Apply(string[] args) {
        string dest=null;string backup=null;bool replaced=false;
        try {
            if(args.Length!=5)throw new Exception("Güncelleme parametreleri geçersiz.");
            int pid=Int32.Parse(args[1]);string staged=Path.GetFullPath(args[2]);dest=Path.GetFullPath(args[3]);
            if(Path.GetFileName(dest)!="NiyazisugarProxy.exe")throw new Exception("Hedef uygulama geçersiz.");
            try {using(Process parent=Process.GetProcessById(pid))if(!parent.WaitForExit(30000))throw new Exception("Uygulama kapanmadı. Güncelleme iptal edildi.");}catch(ArgumentException){}
            if(!String.Equals(Hash(staged),args[4],StringComparison.OrdinalIgnoreCase))throw new Exception("Dosya doğrulaması başarısız.");
            backup=dest+".previous";
            string incoming=dest+".incoming";File.Copy(staged,incoming,true);
            if(File.Exists(dest))File.Replace(incoming,dest,backup,true);else File.Move(incoming,dest);
            replaced=true;
            Process.Start(new ProcessStartInfo {FileName=dest,WorkingDirectory=Path.GetDirectoryName(dest),UseShellExecute=true});
            // Keep the last executable as a manual rollback option.
            File.Delete(staged);
        }catch(Exception e) {
            if(replaced && backup!=null && File.Exists(backup))try {File.Copy(backup,dest,true);}catch(Exception){}
            MessageBox.Show("Güncelleme tamamlanamadı: "+e.Message+"\nÖnceki sürüm yedeği: "+backup,"NiyazisugarProxy");
        }
    }
}
