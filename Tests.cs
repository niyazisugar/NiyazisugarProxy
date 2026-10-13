using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
class Tests {
    static void Assert(bool ok) {if(!ok)throw new Exception("Assertion failed");}
    static string Release(string version,string url,string digest,bool preview) {
        return new JavaScriptSerializer().Serialize(new Dictionary<string,object> {
            {"tag_name",version},{"draft",false},{"prerelease",preview},
            {"assets",new object[]{new Dictionary<string,object>{{"name","NiyazisugarProxy.exe"},{"browser_download_url",url},{"digest",digest}}}}
        });
    }
    static void Reject(Action action) {try {action();}catch(Exception){return;}throw new Exception("Expected rejection");}
    public static int Main(string[] args) {
        try {
            if(args.Length==2&&args[0]=="--config-check") {
                var adapter=NetworkRoutes.Select();
                foreach(string fallback in new string[]{"","185.128.114.39"}) {
                    var config=RoutingConfig.Build(new ProxyItem{Host="192.0.2.1",Port=1080,User="test",Password="example"},adapter.alias,new string[]{"(?i)^C:\\\\Games\\\\metin2client\\.bin$"},fallback);
                    string checkFile=Path.GetTempFileName();
                    try {
                        File.WriteAllText(checkFile,new JavaScriptSerializer().Serialize(config));
                        using(var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args[1],"check -c "+Updates.Quote(checkFile)){UseShellExecute=false,RedirectStandardError=true,RedirectStandardOutput=true,CreateNoWindow=true})) {
                            var err=process.StandardError.ReadToEndAsync();var output=process.StandardOutput.ReadToEndAsync();
                            if(!process.WaitForExit(15000)){process.Kill();throw new Exception("Engine check timeout");}
                            Console.WriteLine(err.GetAwaiter().GetResult()+output.GetAwaiter().GetResult());Assert(process.ExitCode==0);
                        }
                    }finally {File.Delete(checkFile);}
                }
                Console.WriteLine("Actual sing-box config validation passed with and without fallback.");return 0;
            }
            Assert(RoutingConfig.ValidateFallback("")==null);
            Assert(RoutingConfig.ValidateFallback("185.128.114.39")=="185.128.114.39/32");
            Reject(delegate {RoutingConfig.ValidateFallback("bad-ip");});
            var routes=new NetworkRoutes.Route[]{new NetworkRoutes.Route{Index=1,Metric=1,NextHop=0},new NetworkRoutes.Route{Index=2,Metric=100,NextHop=1},new NetworkRoutes.Route{Index=3,Metric=10,NextHop=1}};
            Assert(NetworkRoutes.Choose(routes,new HashSet<uint>{1,2,3}).Index==3);
            var current=new Version(1,0,0,0);string url="https://github.com/niyazisugar/NiyazisugarProxy/releases/download/v1.0.1/NiyazisugarProxy.exe";
            string digest="sha256:"+new string('a',64);
            Assert(Updates.Parse(Release("v1.0.0",url,digest,false),current)==null);
            Assert(Updates.Parse(Release("v0.9.0",url,digest,false),current)==null);
            Assert(Updates.Parse(Release("v1.0.1",url,digest,true),current)==null);
            Assert(Updates.Parse(Release("v1.0.1",url,digest,false),current).Version==new Version(1,0,1));
            Reject(delegate {Updates.Parse(Release("v1.0.1","https://example.com/app.exe",digest,false),current);});
            Reject(delegate {Updates.Parse(Release("v1.0.1",url,null,false),current);});
            Reject(delegate {Updates.Parse(Release("v1.0.1",url,"sha256:bad",false),current);});
            string file=Path.GetTempFileName();try {File.WriteAllText(file,"abc",new System.Text.UTF8Encoding(false));Assert(Updates.Hash(file)=="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");}finally{File.Delete(file);}
            string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try {
                string launcher=Path.Combine(dir,"#Launcher.exe"),bin=Path.Combine(dir,"metin2client.bin"),other=Path.Combine(dir,"sezar2client.bin");
                File.WriteAllText(launcher,"");File.WriteAllText(bin,"");File.WriteAllText(other,"");
                string[] patterns=ClientFiles.Patterns(new string[]{bin,other},launcher);
                Assert(patterns.Length==3);
                bool match=false;foreach(string pattern in patterns)if(System.Text.RegularExpressions.Regex.IsMatch(bin,pattern))match=true;Assert(match);
                foreach(string pattern in patterns)Assert(!System.Text.RegularExpressions.Regex.IsMatch(Path.Combine(dir,"different", "metin2client.bin"),pattern));
                Assert(ClientFiles.Supported(bin));Assert(!ClientFiles.Supported("data.txt"));
            }finally{Directory.Delete(dir,true);}
            Console.WriteLine("BIN selection and separate-directory routing tests passed.");
            Console.WriteLine("Update parsing, rollback version rejection, trusted URL and SHA256 tests passed.");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
