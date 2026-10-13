using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
class DiagnosticSettings {public string Endpoint {get;set;} public string Key {get;set;}}
static class Diagnostics {
    static string SettingsFile {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NiyazisugarProxy","diagnostics.dpapi");}}
    public static string Clean(string text,IEnumerable<ProxyItem> profiles) {
        text=Regex.Replace(text,"\\x1B\\[[0-?]*[ -/]*[@-~]","");
        foreach(var p in profiles){if(!String.IsNullOrEmpty(p.Password))text=text.Replace(p.Password,"[PASSWORD]");if(!String.IsNullOrEmpty(p.User))text=text.Replace(p.User,"[USER]");}
        string user=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);if(!String.IsNullOrEmpty(user))text=Regex.Replace(text,Regex.Escape(user),"[USERPROFILE]",RegexOptions.IgnoreCase);
        text=Regex.Replace(text,@"\b(?:gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+)\b","[TOKEN]");
        text=Regex.Replace(text,@"(?im)(authorization\s*[:=]\s*)[^\r\n]+","$1[HIDDEN]");
        return text.Length>38000?text.Substring(text.Length-38000):text;
    }
    public static void Save(DiagnosticSettings settings) {
        Uri uri;if(!Uri.TryCreate(settings.Endpoint,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.AbsolutePath!="/reports"||!String.IsNullOrEmpty(uri.Query)||!String.IsNullOrEmpty(uri.UserInfo)||String.IsNullOrWhiteSpace(settings.Key))throw new Exception("HTTPS gönderme adresini ve bağlantı kodunu kontrol edin.");
        string file=SettingsFile;Directory.CreateDirectory(Path.GetDirectoryName(file));
        byte[] clear=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(settings));
        try{File.WriteAllBytes(file,ProtectedData.Protect(clear,null,DataProtectionScope.CurrentUser));}finally{Array.Clear(clear,0,clear.Length);}
    }
    public static DiagnosticSettings Load() {
        if(!File.Exists(SettingsFile))return null;
        byte[] clear=ProtectedData.Unprotect(File.ReadAllBytes(SettingsFile),null,DataProtectionScope.CurrentUser);
        try{return new JavaScriptSerializer().Deserialize<DiagnosticSettings>(Encoding.UTF8.GetString(clear));}finally{Array.Clear(clear,0,clear.Length);}
    }
    class TimedClient:WebClient {
        protected override WebRequest GetWebRequest(Uri address){var r=base.GetWebRequest(address);r.Timeout=20000;var http=r as HttpWebRequest;if(http!=null)http.AllowAutoRedirect=false;return r;}
    }
    public static int Send(DiagnosticSettings settings,string report) {
        ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
        using(var c=new TimedClient()) {
            c.Encoding=Encoding.UTF8;c.Headers["Content-Type"]="application/json";c.Headers["Authorization"]="Bearer "+settings.Key;
            var payload=new Dictionary<string,object>{{"version",Updates.Current.ToString(3)},{"log",report}};
            string result=c.UploadString(settings.Endpoint,"POST",new JavaScriptSerializer().Serialize(payload));
            var receipt=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(result);
            int number=Convert.ToInt32(receipt["number"]);if(number<=0)throw new Exception("Gönderme sonucu geçersiz.");return number;
        }
    }
}
