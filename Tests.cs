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
    static System.Windows.Forms.Button FindButton(System.Windows.Forms.Control root,string text){foreach(System.Windows.Forms.Control c in root.Controls){var b=c as System.Windows.Forms.Button;if(b!=null&&b.Text==text)return b;var result=FindButton(c,text);if(result!=null)return result;}return null;}
    static System.Windows.Forms.ComboBox FindTheme(System.Windows.Forms.Control root){foreach(System.Windows.Forms.Control c in root.Controls){var b=c as System.Windows.Forms.ComboBox;if(b!=null&&b.Items.Contains("Karanlık"))return b;var result=FindTheme(c);if(result!=null)return result;}return null;}
    static System.Windows.Forms.CheckBox FindSecond(System.Windows.Forms.Control root){foreach(System.Windows.Forms.Control c in root.Controls){var b=c as System.Windows.Forms.CheckBox;if(b!=null&&b.Text=="2. client etkin")return b;var result=FindSecond(c);if(result!=null)return result;}return null;}
    static void SavePreview(System.Windows.Forms.Form form,string path){using(var bitmap=new System.Drawing.Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,form.ClientRectangle);bitmap.Save(path);}}
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
                using(var live=new LiveRouting()){
                    live.Update(new ClientRoute[]{new ClientRoute{Proxy=new ProxyItem{Host="192.0.2.1",Port=1080},Patterns=new string[]{"^example-one$"},IP="203.0.113.8",Tag="game-proxy-1"},null});
                    string liveFile=Path.GetTempFileName();try{File.WriteAllText(liveFile,new JavaScriptSerializer().Serialize(live.Config(new int[]{12345,12346},adapter.alias)));using(var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args[1],"check -c "+Updates.Quote(liveFile)){UseShellExecute=false,RedirectStandardError=true,RedirectStandardOutput=true,CreateNoWindow=true})){var err=process.StandardError.ReadToEndAsync();var output=process.StandardOutput.ReadToEndAsync();if(!process.WaitForExit(15000)){process.Kill();throw new Exception("Live engine check timeout");}Console.WriteLine(err.GetAwaiter().GetResult()+output.GetAwaiter().GetResult());Assert(process.ExitCode==0);}}finally{File.Delete(liveFile);}
                }
                RelayTests.VerifyLiveEngine(args[1],adapter.alias);
                Console.WriteLine("Actual sing-box config validation passed with and without fallback.");return 0;
            }
            Assert(WindowGeometry.Restore(0,0,new System.Drawing.Size(1920,1080))==new System.Drawing.Size(840,650));
            Assert(WindowGeometry.Restore(4000,4000,new System.Drawing.Size(1366,768))==new System.Drawing.Size(1326,728));
            Assert(WindowGeometry.Restore(1,1,new System.Drawing.Size(1920,1080))==new System.Drawing.Size(640,520));
            Assert(WindowGeometry.HitTest(new System.Drawing.Point(2,2),new System.Drawing.Size(840,650),10)==13);
            Assert(WindowGeometry.HitTest(new System.Drawing.Point(839,649),new System.Drawing.Size(840,650),10)==17);
            Assert(WindowGeometry.HitTest(new System.Drawing.Point(200,300),new System.Drawing.Size(840,650),10)==1);
            var logBuffer=new EngineLogBuffer();for(int n=0;n<100000;n++)logBuffer.Add("Flood log "+n);Assert(logBuffer.Count==500);string firstBatch=logBuffer.Drain();Assert(firstBatch.Contains("99500"));Assert(logBuffer.Count==400);while(logBuffer.Count>0)logBuffer.Drain();Assert(logBuffer.Drain()=="");
            logBuffer.Add(new string('x',100000));Assert(logBuffer.Drain().Length<4200);Console.WriteLine("Log flood queue bound, batch drain and line-size tests passed.");
            string vaultDir=Path.Combine(Path.GetTempPath(),"ProxyVaultTest-"+Guid.NewGuid().ToString("N"));
            try{
                string vaultFile=Path.Combine(vaultDir,"proxies.dat");var saved=new ProxySnapshot{Items=new ProxyItem[]{ProxyItem.Parse("192.0.2.1:1080:vault-user:vault-password"),ProxyItem.Parse("192.0.2.2:1081")},Selected=new int[]{1,0}};
                ProxyVault.SaveTo(vaultFile,saved);var restored=ProxyVault.LoadFrom(vaultFile);Assert(restored.Items.Length==2&&restored.Items[0].Password=="vault-password"&&restored.Items[0].User=="vault-user"&&restored.Selected[0]==1);
                string raw=System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(vaultFile));Assert(!raw.Contains("vault-user")&&!raw.Contains("vault-password")&&!raw.Contains("192.0.2.1"));
                saved.Items=new ProxyItem[]{saved.Items[1]};saved.Selected=new int[]{0,0};ProxyVault.SaveTo(vaultFile,saved);Assert(ProxyVault.LoadFrom(vaultFile).Items.Length==1);
                File.WriteAllText(vaultFile,"corrupted");Reject(delegate{ProxyVault.LoadFrom(vaultFile);});Assert(File.ReadAllText(vaultFile)=="corrupted");
                Console.WriteLine("DPAPI proxy list persistence, selected proxy restoration and encrypted-file tests passed.");
            }finally{if(Directory.Exists(vaultDir))Directory.Delete(vaultDir,true);}
            RelayTests.Run();
            string prefDir=Path.Combine(Path.GetTempPath(),"PreferencesTest-"+Guid.NewGuid().ToString("N"));
            try {
                var pref=new AppPreferences();pref.PutServer("Test server","203.0.113.5");pref.PutServer("TEST SERVER","203.0.113.6");
                Assert(pref.Servers.Count==2);pref.WindowWidth=720;pref.WindowHeight=560;pref.Light=true;pref.Accent=2;pref.SecondEnabled=true;pref.Slots[1].IP="203.0.113.6";
                string prefFile=Path.Combine(prefDir,"preferences.json");pref.SaveTo(prefFile);var loaded=AppPreferences.LoadFrom(prefFile);
                Assert(loaded.WindowWidth==720&&loaded.WindowHeight==560);Assert(loaded.Light&&loaded.Accent==2&&loaded.SecondEnabled&&loaded.Slots[1].IP=="203.0.113.6");Assert(loaded.Servers[1].IP=="203.0.113.6");
                loaded.Light=false;loaded.SaveTo(prefFile);Assert(!AppPreferences.LoadFrom(prefFile).Light);
                Reject(delegate{pref.PutServer("Invalid","not-ip");});
            }finally{if(Directory.Exists(prefDir))Directory.Delete(prefDir,true);}
            using(var guarded=new LiveRouting()){
                var config=guarded.Config(new int[]{12345,12346},"Ethernet");var tun=(Dictionary<string,object>)((object[])config["inbounds"])[0];var addresses=(string[])tun["address"];Assert(addresses.Length==1&&addresses[0]=="172.28.250.1/30");
                var guardedRules=(object[])((Dictionary<string,object>)config["route"])["rules"];var self=(Dictionary<string,object>)guardedRules[0];Assert((string)self["action"]=="reject"&&((string[])self["process_name"])[0]=="sing-box.exe"&&((string[])self["inbound"])[0]=="metin-tun");
                Assert(((Dictionary<string,object>)((object[])config["outbounds"])[0])["bind_interface"].Equals("Ethernet"));
                Console.WriteLine("IPv4-only TUN and TUN-scoped engine self-loop guard tests passed.");
            }
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
                using(var form=new MainForm(true)){
                    form.Show();System.Windows.Forms.Application.DoEvents();Assert(form.ClientSize.Width<=840);
                    SavePreview(form,"ui-preview.png");
                    form.ClientSize=new System.Drawing.Size(640,520);System.Windows.Forms.Application.DoEvents();SavePreview(form,"ui-compact.png");Assert(form.ClientSize.Width==640);
                    FindSecond(form).Checked=true;System.Windows.Forms.Application.DoEvents();SavePreview(form,"ui-two-compact.png");
                    form.ClientSize=new System.Drawing.Size(1000,780);System.Windows.Forms.Application.DoEvents();SavePreview(form,"ui-large.png");
                    FindButton(form,"Ayarlar").PerformClick();System.Windows.Forms.Application.DoEvents();SavePreview(form,"ui-settings.png");
                    FindButton(form,"Loglar").PerformClick();System.Windows.Forms.Application.DoEvents();SavePreview(form,"ui-logs.png");
                    FindButton(form,"Ayarlar").PerformClick();var themeBox=FindTheme(form);Assert(themeBox!=null);themeBox.SelectedIndex=1;System.Windows.Forms.Application.DoEvents();SavePreview(form,"ui-light.png");
                    form.Close();
                }
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

