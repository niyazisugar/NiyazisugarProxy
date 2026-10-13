using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
static class EngineBootstrap {
    const string Expected="1.14.2";
    const string ArchiveHash="c2d8bfff918755808781dfdeeb8581b6c91eb3a243d9a7b55483cfc0c0684d32";
    static bool Correct(string path) {
        if(!File.Exists(path))return false;
        try {
            using(var p=Process.Start(new ProcessStartInfo(path,"version"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})) {
                var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();
                if(!p.WaitForExit(5000)){p.Kill();return false;}
                return p.ExitCode==0&&output.GetAwaiter().GetResult().Contains("sing-box version "+Expected);
            }
        }catch(Exception){return false;}
    }
    public static string Ensure(string bundled,Action<string> log) {
        if(Correct(bundled))return bundled;
        string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NiyazisugarProxy","engine",Expected);
        Directory.CreateDirectory(dir);string installed=Path.Combine(dir,"sing-box.exe");
        if(Correct(installed))return installed;
        log("Yeni yönlendirme için sing-box "+Expected+" indiriliyor; SHA256 doğrulanacak.");
        string archive=Path.Combine(dir,Guid.NewGuid().ToString("N")+".zip");string temporary=installed+".incoming";
        try {
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            using(var c=new WebClient()) {c.Headers["User-Agent"]="NiyazisugarProxy";c.DownloadFile("https://github.com/SagerNet/sing-box/releases/download/v1.14.2/sing-box-1.14.2-windows-amd64.zip",archive);}
            if(Updates.Hash(archive)!=ArchiveHash)throw new Exception("Motor arşivi doğrulanamadı.");
            using(var zip=ZipFile.OpenRead(archive)) {
                ZipArchiveEntry exe=null;
                foreach(var entry in zip.Entries)if(entry.Name=="sing-box.exe") {if(exe!=null)throw new Exception("Motor arşivi belirsiz.");exe=entry;}
                if(exe==null||exe.Length>100*1024*1024)throw new Exception("Motor arşivi geçersiz.");
                exe.ExtractToFile(temporary,true);
            }
            if(!Correct(temporary))throw new Exception("Motor sürümü doğrulanamadı.");
            if(File.Exists(installed))File.Replace(temporary,installed,null);else File.Move(temporary,installed);
            return installed;
        }finally {if(File.Exists(archive))File.Delete(archive);if(File.Exists(temporary))File.Delete(temporary);}
    }
}
