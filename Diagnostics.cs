using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
static class Diagnostics {
    public static string Folder() {
        string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if(String.IsNullOrWhiteSpace(desktop))throw new Exception("Windows Masaüstü konumu bulunamadı.");
        string folder=Path.Combine(desktop,"NiyazisugarProxy-Loglar");
        Directory.CreateDirectory(folder);return folder;
    }
    public static string CreateSession() {return CreateSessionAt(Folder());}
    public static string CreateSessionAt(string folder) {
        Directory.CreateDirectory(folder);
        string file=Path.Combine(folder,"NiyazisugarProxy-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".txt");
        using(var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
        using(var writer=new StreamWriter(stream,new UTF8Encoding(true)))
            writer.WriteLine("NiyazisugarProxy v"+Updates.Current.ToString(3)+" — yerel uygulama kaydı");
        return file;
    }
    public static string Clean(string text,IEnumerable<ProxyItem> profiles) {
        text=Regex.Replace(text,"\\x1B\\[[0-?]*[ -/]*[@-~]","");
        foreach(var p in profiles){if(!String.IsNullOrEmpty(p.Password))text=text.Replace(p.Password,"[PASSWORD]");if(!String.IsNullOrEmpty(p.User))text=text.Replace(p.User,"[USER]");}
        string user=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if(!String.IsNullOrEmpty(user))text=Regex.Replace(text,Regex.Escape(user),"[USERPROFILE]",RegexOptions.IgnoreCase);
        text=Regex.Replace(text,@"\b(?:gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+)\b","[TOKEN]");
        text=Regex.Replace(text,@"(?im)(authorization\s*[:=]\s*)[^\r\n]+","$1[HIDDEN]");
        text=Regex.Replace(text,@"\b(?:\d{1,3}\.){3}\d{1,3}:\d+:[^\s:]+:[^\s]+","[PROXY-CREDENTIALS]");
        return text;
    }
    public static void Append(string path,string text,IEnumerable<ProxyItem> profiles) {
        File.AppendAllText(path,Clean(text,profiles).TrimEnd()+Environment.NewLine,new UTF8Encoding(false));
    }
}
