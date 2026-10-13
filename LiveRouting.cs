using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
sealed class LiveRouting : IDisposable {
    readonly string directory;readonly JavaScriptSerializer json=new JavaScriptSerializer();
    public string[] Paths;
    public LiveRouting(){directory=Path.Combine(Path.GetTempPath(),"NiyazisugarLive-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);Paths=new string[5];for(int i=0;i<5;i++){Paths[i]=Path.Combine(directory,"rules-"+i+".json");Write(i,new object[0]);}}
    void Write(int index,object[] rules){string temp=Paths[index]+".tmp";File.WriteAllText(temp,json.Serialize(RoutingConfig.D("version",3,"rules",rules)),new UTF8Encoding(false));if(File.Exists(Paths[index]))File.Replace(temp,Paths[index],null);else File.Move(temp,Paths[index]);}
    public void Update(ClientRoute[] clients){
        // Clear unknown-process fallbacks before changing ownership of a shared destination.
        Write(2,new object[0]);Write(3,new object[0]);Write(4,new object[0]);
        for(int i=0;i<2;i++){var c=clients[i];var rules=new List<object>();if(c!=null){var match=RoutingConfig.D("process_path_regex",c.Patterns);string cidr=RoutingConfig.ValidateFallback(c.IP);if(cidr!=null)match.Add("ip_cidr",new string[]{cidr});rules.Add(match);rules.Add(RoutingConfig.D("process_path_regex",c.Patterns,"ip_cidr",new string[]{"::/0"}));}Write(i,rules.ToArray());}
        bool shared=clients[0]!=null&&clients[1]!=null&&RoutingConfig.SharedIP(clients);
        if(shared)Write(4,new object[]{RoutingConfig.D("ip_cidr",new string[]{RoutingConfig.ValidateFallback(clients[0].IP)})});
        else for(int i=0;i<2;i++)if(clients[i]!=null){string cidr=RoutingConfig.ValidateFallback(clients[i].IP);if(cidr!=null)Write(i+2,new object[]{RoutingConfig.D("ip_cidr",new string[]{cidr})});}
    }
    public Dictionary<string,object> Config(int[] ports,string iface){
        var config=RoutingConfig.Build(new ProxyItem{Host="127.0.0.1",Port=ports[0],User="",Password=""},iface,new string[]{"^$"},"");
        config["outbounds"]=new object[]{RoutingConfig.D("type","direct","tag","direct","bind_interface",iface),RoutingConfig.D("type","socks","tag","game-proxy-1","server","127.0.0.1","server_port",ports[0],"version","5"),RoutingConfig.D("type","socks","tag","game-proxy-2","server","127.0.0.1","server_port",ports[1],"version","5")};
        var route=(Dictionary<string,object>)config["route"];route.Remove("default_interface");var sets=new List<object>();for(int i=0;i<5;i++)sets.Add(RoutingConfig.D("type","local","tag","slot-rules-"+i,"format","source","path",Paths[i]));route["rule_set"]=sets.ToArray();
        var rules=new List<object>{RoutingConfig.D("inbound",new string[]{"metin-tun"},"process_name",new string[]{"sing-box.exe"},"action","reject"),RoutingConfig.D("port",new int[]{53},"action","hijack-dns")};
        for(int i=0;i<2;i++){rules.Add(RoutingConfig.D("rule_set",new string[]{"slot-rules-"+i},"ip_version",6,"action","reject"));rules.Add(RoutingConfig.D("rule_set",new string[]{"slot-rules-"+i},"network","tcp","action","route","outbound","game-proxy-"+(i+1)));rules.Add(RoutingConfig.D("rule_set",new string[]{"slot-rules-"+i},"action","reject"));}
        rules.Add(RoutingConfig.D("rule_set",new string[]{"slot-rules-4"},"network","tcp","action","reject"));
        for(int i=0;i<2;i++)rules.Add(RoutingConfig.D("rule_set",new string[]{"slot-rules-"+(i+2)},"network","tcp","action","route","outbound","game-proxy-"+(i+1)));
        route["rules"]=rules.ToArray();return config;
    }
    public void Dispose(){try{Directory.Delete(directory,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
}
