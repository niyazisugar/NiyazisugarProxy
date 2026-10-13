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
    [STAThread] public static int Main(string[] args) {
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
                foreach(bool shared in new bool[]{false,true}) {
                    var dual=RoutingConfig.BuildMany(new ClientRoute[]{new ClientRoute{Proxy=new ProxyItem{Host="192.0.2.1",Port=1080},Patterns=new string[]{"(?i)^C:\\\\Games1\\\\client.exe$"},IP="203.0.113.8",Tag="game-proxy-1"},new ClientRoute{Proxy=new ProxyItem{Host="192.0.2.2",Port=1081},Patterns=new string[]{"(?i)^C:\\\\Games2\\\\client.exe$"},IP=shared?"203.0.113.8":"203.0.113.9",Tag="game-proxy-2"}},adapter.alias);
                    string dualFile=Path.GetTempFileName();try{File.WriteAllText(dualFile,new JavaScriptSerializer().Serialize(dual));using(var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args[1],"check -c "+Updates.Quote(dualFile)){UseShellExecute=false,RedirectStandardError=true,RedirectStandardOutput=true,CreateNoWindow=true})){var err=process.StandardError.ReadToEndAsync();var output=process.StandardOutput.ReadToEndAsync();if(!process.WaitForExit(15000)){process.Kill();throw new Exception("Dual engine check timeout");}Console.WriteLine(err.GetAwaiter().GetResult()+output.GetAwaiter().GetResult());Assert(process.ExitCode==0);}}finally{File.Delete(dualFile);}
                }
                Console.WriteLine("Actual sing-box config validation passed with and without fallback.");return 0;
            }
            string prefDir=Path.Combine(Path.GetTempPath(),"PreferencesTest-"+Guid.NewGuid().ToString("N"));
            try {
                var pref=new AppPreferences();pref.PutServer("Test server","203.0.113.5");pref.PutServer("TEST SERVER","203.0.113.6");
                Assert(pref.Servers.Count==2);pref.Light=true;pref.Accent=2;pref.SecondEnabled=true;pref.Slots[1].IP="203.0.113.6";
                string prefFile=Path.Combine(prefDir,"preferences.json");pref.SaveTo(prefFile);var loaded=AppPreferences.LoadFrom(prefFile);
                Assert(loaded.Light&&loaded.Accent==2&&loaded.SecondEnabled&&loaded.Slots[1].IP=="203.0.113.6");Assert(loaded.Servers[1].IP=="203.0.113.6");
                loaded.Light=false;loaded.SaveTo(prefFile);Assert(!AppPreferences.LoadFrom(prefFile).Light);
                Reject(delegate{pref.PutServer("Invalid","not-ip");});
            }finally{if(Directory.Exists(prefDir))Directory.Delete(prefDir,true);}
            var pair=new ClientRoute[]{
                new ClientRoute{Proxy=new ProxyItem{Host="192.0.2.1",Port=1080,User="one",Password="test"},Patterns=new string[]{"(?i)^C:\\\\Games1\\\\client.exe$"},IP="203.0.113.8",Tag="game-proxy-1"},
                new ClientRoute{Proxy=new ProxyItem{Host="192.0.2.2",Port=1081,User="two",Password="test"},Patterns=new string[]{"(?i)^C:\\\\Games2\\\\client.exe$"},IP="203.0.113.8",Tag="game-proxy-2"}
            };
            var multiple=RoutingConfig.BuildMany(pair,"Ethernet");
            Assert(((object[])multiple["outbounds"]).Length==3);Assert(RoutingConfig.SharedIP(pair));
            var ruleArray=(object[])((Dictionary<string,object>)multiple["route"])["rules"];
            var tail=(Dictionary<string,object>)ruleArray[ruleArray.Length-1];Assert((string)tail["action"]=="reject");
            Assert((string)((Dictionary<string,object>)ruleArray[2])["outbound"]=="game-proxy-1");
            Assert((string)((Dictionary<string,object>)ruleArray[5])["outbound"]=="game-proxy-2");
            pair[1].IP="203.0.113.9";multiple=RoutingConfig.BuildMany(pair,"Ethernet");ruleArray=(object[])((Dictionary<string,object>)multiple["route"])["rules"];
            Assert((string)((Dictionary<string,object>)ruleArray[ruleArray.Length-1])["outbound"]=="game-proxy-2");
            pair[1].Patterns=pair[0].Patterns;Reject(delegate{RoutingConfig.BuildMany(pair,"Ethernet");});
            Console.WriteLine("Saved server/theme roundtrip and dual-client routing/conflict tests passed.");
            if(args.Length==1&&args[0]=="--ui-smoke") {
                System.Windows.Forms.Application.EnableVisualStyles();
                using(var form=new MainForm(true)){form.Show();System.Windows.Forms.Application.DoEvents();Assert(form.ClientSize.Width==1000);using(var bitmap=new System.Drawing.Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,form.ClientRectangle);bitmap.Save("ui-preview.png");}form.Close();}
                Console.WriteLine("Windows UI construction and screenshot passed.");
            }
            string safe=Diagnostics.Clean("username demo password SECRET\n"+Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\nAuthorization: Bearer TEST",new ProxyItem[]{new ProxyItem{User="demo",Password="SECRET"}});
            Assert(!safe.Contains("SECRET")&&!safe.Contains("demo")&&!safe.Contains("Bearer TEST"));
            string exportDirectory=Path.Combine(Path.GetTempPath(),"LocalLogTest-"+Guid.NewGuid().ToString("N"));
            try {
                string first=Diagnostics.CreateSessionAt(exportDirectory);
                string second=Diagnostics.CreateSessionAt(exportDirectory);
                Assert(first!=second);
                Diagnostics.Append(first,"demo SECRET authorization: Bearer FAKE",new ProxyItem[]{new ProxyItem{User="demo",Password="SECRET"}});
                string saved=File.ReadAllText(first);
                Assert(!saved.Contains("demo")&&!saved.Contains("SECRET")&&!saved.Contains("Bearer FAKE"));
                string before=saved;
                Diagnostics.Append(first,"second log line",new ProxyItem[0]);
                Assert(File.ReadAllText(first).StartsWith(before));
                Assert(File.ReadAllText(first).Contains("second log line"));
                Console.WriteLine("Local log creation, append and secret-redaction tests passed.");
            }finally{if(Directory.Exists(exportDirectory))Directory.Delete(exportDirectory,true);}
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

