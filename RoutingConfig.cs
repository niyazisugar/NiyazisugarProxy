// Adapted from buraakoztoprak/socksproject native/NeviSocks/Core.cs at user request.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
class ClientRoute { public ProxyItem Proxy; public string[] Patterns; public string IP,Tag; }
static class RoutingConfig {
    public static Dictionary<string,object> D(params object[] pairs) {
        var result=new Dictionary<string,object>();for(int i=0;i<pairs.Length;i+=2)result.Add((string)pairs[i],pairs[i+1]);return result;
    }
    public static string ValidateFallback(string value) {
        if(String.IsNullOrWhiteSpace(value))return null;
        IPAddress ip;
        if(!IPAddress.TryParse(value.Trim(),out ip)||ip.AddressFamily!=AddressFamily.InterNetwork)throw new Exception("Sunucu IP alanına tek bir IPv4 adresi girin veya boş bırakın.");
        return ip.ToString()+"/32";
    }
    public static Dictionary<string,object> Build(ProxyItem p,string iface,string[] patterns,string fallback) {
        return BuildMany(new ClientRoute[]{new ClientRoute{Proxy=p,Patterns=patterns,IP=fallback,Tag="game-proxy"}},iface);
    }
    public static bool SharedIP(ClientRoute[] clients){return clients.Length==2&&ValidateFallback(clients[0].IP)!=null&&ValidateFallback(clients[0].IP)==ValidateFallback(clients[1].IP);}
    public static Dictionary<string,object> BuildMany(ClientRoute[] clients,string iface) {
        if(clients==null||clients.Length<1||clients.Length>2)throw new Exception("Bir veya iki client seçin.");
        var tags=new HashSet<string>();var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var c in clients){if(c.Proxy==null||c.Patterns==null||c.Patterns.Length==0||String.IsNullOrWhiteSpace(c.Tag)||!tags.Add(c.Tag))throw new Exception("Client yönlendirme profili geçersiz.");foreach(string pattern in c.Patterns)if(!paths.Add(pattern))throw new Exception("Client dosyaları farklı klasörlerde olmalı.");ValidateFallback(c.IP);}
        var rules=new List<object>{D("port",new int[]{53},"action","hijack-dns")};
        var outbounds=new List<object>{D("type","direct","tag","direct","bind_interface",iface)};
        // Process routes precede destination fallbacks, so a known client keeps its own proxy.
        foreach(var c in clients){
            rules.Add(D("process_path_regex",c.Patterns,"ip_version",6,"action","reject"));
            var route=D("process_path_regex",c.Patterns,"action","route","outbound",c.Tag);
            string cidr=ValidateFallback(c.IP);if(cidr!=null)route.Add("ip_cidr",new string[]{cidr});
            rules.Add(route);
            if(cidr!=null)rules.Add(D("process_path_regex",c.Patterns,"action","route","outbound","direct"));
            var p=c.Proxy;outbounds.Add(D("type","socks","tag",c.Tag,"server",p.Host,"server_port",p.Port,"version","5","username",p.User,"password",p.Password,"bind_interface",iface));
        }
        if(SharedIP(clients))rules.Add(D("ip_cidr",new string[]{ValidateFallback(clients[0].IP)},"network","tcp","action","reject"));
        else foreach(var c in clients){string cidr=ValidateFallback(c.IP);if(cidr!=null)rules.Add(D("ip_cidr",new string[]{cidr},"network","tcp","action","route","outbound",c.Tag));}
        return D("log",D("level","info","timestamp",true),
          "inbounds",new object[]{D("type","tun","tag","metin-tun","interface_name","NiyazisugarProxy","address",new string[]{"172.28.250.1/30"},"mtu",1500,"auto_route",true,"strict_route",true,"stack","mixed","dns_mode","hijack")},
          "dns",D("servers",new object[]{D("type","https","tag","direct-dns","server","1.1.1.1","server_port",443,"path","/dns-query","tls",D("enabled",true,"server_name","cloudflare-dns.com"),"detour","direct")},"final","direct-dns","strategy","ipv4_only"),
          "outbounds",outbounds.ToArray(),
          "route",D("default_interface",iface,"find_process",true,"rules",rules.ToArray(),"final","direct"));
    }
}

