// Adapted from buraakoztoprak/socksproject native/NeviSocks/Core.cs at user request.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
        var rules=new List<object>{D("port",new int[]{53},"action","hijack-dns")};
        string cidr=ValidateFallback(fallback);
        if(cidr!=null)rules.Add(D("ip_cidr",new string[]{cidr},"network","tcp","action","route","outbound","game-proxy"));
        rules.Add(D("process_path_regex",patterns,"ip_version",6,"action","reject"));
        rules.Add(D("process_path_regex",patterns,"action","route","outbound","game-proxy"));
        return D("log",D("level","info","timestamp",true),
          "inbounds",new object[]{D("type","tun","tag","metin-tun","interface_name","NiyazisugarProxy","address",new string[]{"172.28.250.1/30","fdfe:dcba:9876::1/126"},"mtu",1500,"auto_route",true,"strict_route",true,"stack","mixed","dns_mode","hijack")},
          "dns",D("servers",new object[]{D("type","https","tag","direct-dns","server","1.1.1.1","server_port",443,"path","/dns-query","tls",D("enabled",true,"server_name","cloudflare-dns.com"),"detour","direct")},"final","direct-dns","strategy","ipv4_only"),
          "outbounds",new object[]{D("type","direct","tag","direct","bind_interface",iface),D("type","socks","tag","game-proxy","server",p.Host,"server_port",p.Port,"version","5","username",p.User,"password",p.Password,"bind_interface",iface)},
          "route",D("default_interface",iface,"find_process",true,"rules",rules.ToArray(),"final","direct"));
    }
}
