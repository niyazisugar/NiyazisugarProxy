using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
class ServerProfile { public string Name {get;set;} public string IP {get;set;} public override string ToString(){return Name+" • "+IP;} }
class SlotPreference {public string Launcher {get;set;} public string[] Files {get;set;} public string IP {get;set;} }
class AppPreferences {
    public int WindowWidth {get;set;} public int WindowHeight {get;set;}
    public bool Light {get;set;} public int Accent {get;set;} public bool SecondEnabled {get;set;}
    public List<ServerProfile> Servers {get;set;}
    public SlotPreference[] Slots {get;set;}
    public AppPreferences(){Servers=new List<ServerProfile>{new ServerProfile{Name="Sezar2",IP="185.128.114.39"}};Slots=new SlotPreference[]{new SlotPreference(),new SlotPreference()};}
    static string FilePath(){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NiyazisugarProxy","preferences.json");}
    public static AppPreferences Load(){try{return LoadFrom(FilePath());}catch{return new AppPreferences();}}
    public static AppPreferences LoadFrom(string path){if(!File.Exists(path))return new AppPreferences();var result=new JavaScriptSerializer().Deserialize<AppPreferences>(File.ReadAllText(path,Encoding.UTF8));if(result==null||result.Servers==null||result.Slots==null||result.Slots.Length!=2||result.Slots[0]==null||result.Slots[1]==null)throw new Exception("Ayar dosyası geçersiz.");foreach(var s in result.Servers){if(s==null||String.IsNullOrWhiteSpace(s.Name)||RoutingConfig.ValidateFallback(s.IP)==null)throw new Exception("Sunucu kaydı geçersiz.");}return result;}
    public void PutServer(string name,string ip){if(String.IsNullOrWhiteSpace(name)||name.Length>80)throw new Exception("Geçerli sunucu adı girin.");string cidr=RoutingConfig.ValidateFallback(ip);if(cidr==null)throw new Exception("Sunucu IP'si gerekli.");var existing=Servers.Find(delegate(ServerProfile x){return x.Name.Equals(name,StringComparison.OrdinalIgnoreCase);});if(existing==null){existing=new ServerProfile{Name=name};Servers.Add(existing);}existing.IP=cidr.Substring(0,cidr.Length-3);}
    public void Save(){SaveTo(FilePath());}
    public void SaveTo(string path){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(this),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
