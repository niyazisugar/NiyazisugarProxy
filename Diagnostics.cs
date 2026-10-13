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
    public static string FailureMessage(int status,string code) {
        switch(code) {
            case "unauthorized": return "HTTP 401: Bağlantı kodu uyuşmuyor. Tanılama ayarlarına Cloudflare INGEST_KEY değerini tekrar girin.";
            case "service_not_configured": return "HTTP 503: Cloudflare servis değişkenleri eksik veya geçersiz.";
            case "repository_unavailable": return "HTTP 502: GitHub anahtarı özel tanılama deposunu okuyamıyor. GITHUB_TOKEN depo erişimini kontrol edin.";
            case "github_write_failed": return "HTTP 502: GitHub kayıt oluşturmayı reddetti. Issues: Read and write iznini ve deponun Issues ayarını kontrol edin.";
            case "private_repository_required": return "HTTP 503: Tanılama deposu Private olmalı.";
            case "not_found": return "HTTP 404: Gönderme adresinin sonunda /reports olmalı.";
            case "too_large": return "HTTP 413: Tanılama kaydı boyut sınırını aşıyor.";
            case "invalid_report": return "HTTP 400: Tanılama kaydı biçimi geçersiz.";
            default: return "Gönderim başarısız. HTTP "+status+". Kayıt oluşturulduğu doğrulanamadı.";
        }
    }
    public static int Send(DiagnosticSettings settings,string report) {
        var payload=new Dictionary<string,object>{{"version",Updates.Current.ToString(3)},{"log",report}};
        var response=NativeHttp.Send(settings.Endpoint,"POST",settings.Key,new JavaScriptSerializer().Serialize(payload));
        if(response.Status!=201) {
            string code="";
            try {
                var error=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(response.Body);
                object value;if(error.TryGetValue("error",out value))code=value as string;
            }catch(Exception){}
            throw new Exception(FailureMessage(response.Status,code));
        }
        try {
            var receipt=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(response.Body);
            int number=Convert.ToInt32(receipt["number"]);
            if(number<=0)throw new Exception();
            return number;
        }catch(Exception){throw new Exception("Servis cevabı geçersiz; kayıt oluşturulduğu doğrulanamadı.");}
    }
}
