using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
class ProxySnapshot {public ProxyItem[] Items {get;set;} public int[] Selected {get;set;}}
static class ProxyVault {
    static readonly byte[] entropy=Encoding.UTF8.GetBytes("NiyazisugarProxy/proxy-list/v1");
    static string FilePath(){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NiyazisugarProxy","proxies.dat");}
    public static ProxySnapshot Load(){return LoadFrom(FilePath());}
    public static void Save(ProxySnapshot value){SaveTo(FilePath(),value);}
    public static ProxySnapshot LoadFrom(string path){
        if(!File.Exists(path))return new ProxySnapshot{Items=new ProxyItem[0],Selected=new int[]{-1,-1}};
        if(new FileInfo(path).Length>4*1024*1024)throw new IOException("Proxy kayıt dosyası çok büyük.");
        byte[] clear=ProtectedData.Unprotect(File.ReadAllBytes(path),entropy,DataProtectionScope.CurrentUser);
        try{var value=new JavaScriptSerializer{MaxJsonLength=4*1024*1024}.Deserialize<ProxySnapshot>(Encoding.UTF8.GetString(clear));Validate(value);return value;}finally{Array.Clear(clear,0,clear.Length);}
    }
    static void Validate(ProxySnapshot value){if(value==null||value.Items==null||value.Selected==null||value.Selected.Length!=2)throw new IOException("Proxy kaydı geçersiz.");foreach(var item in value.Items){if(item==null)throw new IOException("Proxy kaydı geçersiz.");ProxyItem.Parse(item.Host+":"+item.Port);item.User=item.User??"";item.Password=item.Password??"";}}
    public static void SaveTo(string path,ProxySnapshot value){Validate(value);byte[] clear=Encoding.UTF8.GetBytes(new JavaScriptSerializer{MaxJsonLength=4*1024*1024}.Serialize(value));byte[] encrypted;try{encrypted=ProtectedData.Protect(clear,entropy,DataProtectionScope.CurrentUser);}finally{Array.Clear(clear,0,clear.Length);}Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,encrypted);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
