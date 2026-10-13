using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

class ProxyItem {
    public string Host, User, Password;
    public int Port;
    public override string ToString() { return Host + ":" + Port; }
    public static ProxyItem Parse(string input) {
        string[] p=input.Trim().Split(new char[]{':'},4);
        IPAddress ip; int port;
        if ((p.Length!=2 && p.Length!=4) || !IPAddress.TryParse(p[0],out ip) || ip.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork || !Int32.TryParse(p[1],out port) || port<1 || port>65535)
            throw new ArgumentException("Format: IPv4:PORT veya IPv4:PORT:KULLANICI:ŞİFRE");
        return new ProxyItem {Host=p[0],Port=port,User=p.Length==4?p[2]:"",Password=p.Length==4?p[3]:""};
    }
}

class MainForm : Form {
    ComboBox proxies=new ComboBox(); TextBox client=new TextBox(); TextBox logs=new TextBox();
    Label status=new Label(); Button load,add,select,connect,stop,launch,update,sendReport;
    UpdateRelease availableUpdate; bool updateBusy;
    string localLogPath;
    TextBox serverIP=new TextBox();
    string launcherPath; string[] selectedFiles=new string[0];
    Process engine; string configFile; bool starting,closing,ready;
    readonly JavaScriptSerializer json=new JavaScriptSerializer();
    readonly string baseDir=AppDomain.CurrentDomain.BaseDirectory;
    public MainForm() {
        Text="NiyazisugarProxy v"+Updates.Current.ToString(3)+" • Metin2 Client SOCKS5"; ClientSize=new Size(780,625); MinimumSize=new Size(796,664);
        BackColor=Color.FromArgb(37,27,55); ForeColor=Color.FromArgb(237,230,250); Font=new Font("Segoe UI",10);
        Label title=new Label {Text="NIYAZISUGAR PROXY",Font=new Font("Segoe UI",19,FontStyle.Bold),AutoSize=true,Location=new Point(22,18)}; Controls.Add(title);
        Controls.Add(new Label {Text="İstediğiniz sunucunun asıl oyun clientini seçin",AutoSize=true,Location=new Point(24,61)});
        load=Btn("Liste yükle",22,100,120,LoadList); add=Btn("Proxy ekle",640,100,118,AddProxy);
        proxies.SetBounds(150,104,480,30); proxies.DropDownStyle=ComboBoxStyle.DropDownList; Controls.Add(proxies);
        Controls.Add(new Label {Text="Oyun clientleri (.exe / .bin) — birden fazla seçilebilir",AutoSize=true,Location=new Point(24,155)});
        client.SetBounds(22,185,608,30); client.ReadOnly=true; Controls.Add(client); select=Btn("Client seç",640,182,118,SelectClient);
        connect=Btn("Bağlan",22,235,145,Connect); stop=Btn("Durdur",177,235,145,Stop); launch=Btn("Oyunu aç",332,235,145,Launch);
        stop.Enabled=false; launch.Enabled=false;
        update=Btn("Güncelleme kontrol",487,235,271,CheckUpdates);
        Shown+=delegate {CheckUpdates(null,EventArgs.Empty);};
        status.SetBounds(22,288,736,42); status.Text="Durum: Bağlı değil"; Controls.Add(status);
        Controls.Add(new Label {Text="Sunucu IP kuralı (isteğe bağlı):",AutoSize=true,Location=new Point(22,332)});
        serverIP.SetBounds(245,328,180,28);Controls.Add(serverIP);
        Controls.Add(new Label {Text="Sezar2: 185.128.114.39",AutoSize=true,Location=new Point(443,332)});
        Controls.Add(new Label {Text="IP girilirse o adrese giden tüm TCP bağlantıları proxy kullanır. Boş bırakılabilir.",AutoSize=true,Location=new Point(22,367)});
        logs.SetBounds(22,403,736,153); logs.Multiline=true; logs.ReadOnly=true; logs.ScrollBars=ScrollBars.Vertical;
        logs.BackColor=Color.FromArgb(23,15,37); logs.ForeColor=ForeColor; logs.Font=new Font("Consolas",9); logs.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right; Controls.Add(logs);
        sendReport=Btn("Log klasörünü aç",22,574,230,OpenLogs);
        sendReport.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;
        FormClosing+=OnClosing;
        try {localLogPath=Diagnostics.CreateSession();Log("Yerel log kaydı başladı.");}
        catch(Exception ex){logs.AppendText("Log klasörü oluşturulamadı: "+ex.Message+Environment.NewLine);}
    }
    Button Btn(string text,int x,int y,int width,EventHandler action) {
        Button b=new Button {Text=text,Location=new Point(x,y),Size=new Size(width,36),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(60,44,80),ForeColor=ForeColor};
        b.Click+=action; Controls.Add(b); return b;
    }
    void UI(Action a) { if(IsDisposed || closing)return; try { BeginInvoke(a); } catch(InvalidOperationException){} }
    void Log(string text) {
        if(String.IsNullOrEmpty(text))return;
        foreach(ProxyItem p in proxies.Items) {
            if(!String.IsNullOrEmpty(p.Password))text=text.Replace(p.Password,"[gizli]");
            if(!String.IsNullOrEmpty(p.User))text=text.Replace(p.User,"[gizli]");
        }
        if(localLogPath!=null) {
            try {
                var profiles=new List<ProxyItem>();foreach(ProxyItem p in proxies.Items)profiles.Add(p);
                if(new FileInfo(localLogPath).Length>10*1024*1024)localLogPath=Diagnostics.CreateSession();
                Diagnostics.Append(localLogPath,text,profiles);
            }catch(Exception ex){localLogPath=null;logs.AppendText("Log dosyasına yazılamadı: "+ex.Message+Environment.NewLine);}
        }
        if(logs.TextLength>120000)logs.Clear(); logs.AppendText(text.TrimEnd()+Environment.NewLine);
    }
    void Error(Exception e) {MessageBox.Show(this,e.Message,"NiyazisugarProxy",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    void LoadList(object sender,EventArgs e) {
        using(OpenFileDialog d=new OpenFileDialog {Filter="Proxy listesi (*.txt)|*.txt|Tüm dosyalar|*.*"}) {
            if(d.ShowDialog()!=DialogResult.OK)return;
            try {
                var list=new List<ProxyItem>(); int n=0;
                foreach(string line in File.ReadAllLines(d.FileName,Encoding.UTF8)) {
                    n++; if(String.IsNullOrWhiteSpace(line)||line.TrimStart().StartsWith("#"))continue;
                    try {list.Add(ProxyItem.Parse(line));} catch {throw new ArgumentException(n+". satırın proxy formatı geçersiz.");}
                }
                if(list.Count==0)throw new ArgumentException("Liste boş.");
                proxies.Items.Clear(); proxies.Items.AddRange(list.ToArray()); proxies.SelectedIndex=0; Log(list.Count+" proxy yüklendi.");
            } catch(Exception ex) {Error(ex);}
        }
    }
    void AddProxy(object sender,EventArgs e) {
        using(Form d=new Form {Text="Proxy ekle",ClientSize=new Size(480,125),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
            var label=new Label {Text="IPv4:PORT veya IPv4:PORT:KULLANICI:ŞİFRE",AutoSize=true,Location=new Point(12,12)};
            var input=new TextBox {Location=new Point(12,40),Width=450,UseSystemPasswordChar=true};
            var ok=new Button {Text="Ekle",Location=new Point(362,78),DialogResult=DialogResult.OK};
            d.Controls.Add(label);d.Controls.Add(input);d.Controls.Add(ok);d.AcceptButton=ok;
            if(d.ShowDialog(this)==DialogResult.OK)try {proxies.Items.Add(ProxyItem.Parse(input.Text));proxies.SelectedIndex=proxies.Items.Count-1;}catch(Exception ex){Error(ex);}
        }
    }
    void SelectClient(object sender,EventArgs e) {
        using(OpenFileDialog d=new OpenFileDialog {Title="Oyun clientlerini seçin",Multiselect=true,Filter="Oyun clientleri (*.exe;*.bin)|*.exe;*.bin|EXE dosyaları|*.exe|BIN dosyaları|*.bin"}) {
            if(d.ShowDialog()!=DialogResult.OK)return;
            string launcher=null;
            foreach(string p in d.FileNames)if(Path.GetExtension(p).Equals(".exe",StringComparison.OrdinalIgnoreCase)) {
                if(launcher!=null) {MessageBox.Show(this,"Yalnızca bir launcher EXE seçin; BIN dosyalarıyla birlikte seçebilirsiniz.");return;}
                launcher=Path.GetFullPath(p);
            }
            if(launcher==null)using(OpenFileDialog l=new OpenFileDialog {Title="Oyunu açacak launcher EXE'yi seçin",InitialDirectory=Path.GetDirectoryName(d.FileNames[0]),Filter="Launcher (*.exe)|*.exe"}) {
                if(l.ShowDialog()!=DialogResult.OK)return;launcher=Path.GetFullPath(l.FileName);
            }
            try {
                string[] patterns=ClientFiles.Patterns(d.FileNames,launcher);
                selectedFiles=d.FileNames;launcherPath=launcher;client.Text=String.Join(" ; ",d.FileNames);
                Log("Launcher: "+launcherPath);
                Log("Yönlendirilecek client sayısı: "+patterns.Length);
            }catch(Exception ex){Error(ex);}
        }
    }
    void SetBusy(bool busy) {
        load.Enabled=add.Enabled=select.Enabled=connect.Enabled=proxies.Enabled=serverIP.Enabled=!busy;
        update.Enabled=!busy&&!updateBusy;
        stop.Enabled=busy&&!starting; launch.Enabled=busy&&ready;
    }
    string Config(string exe,ProxyItem p) {
        var adapter=NetworkRoutes.Select();
        Log("Fiziksel ağ arayüzü: "+adapter.alias+"; geçit: "+adapter.gateway);
        if(!String.IsNullOrWhiteSpace(serverIP.Text))Log("Sunucu IP kuralı: "+serverIP.Text+" (tüm uygulamalar için TCP hedef eşleşmesi)");
        return json.Serialize(RoutingConfig.Build(p,adapter.alias,ClientFiles.Patterns(selectedFiles,launcherPath),serverIP.Text));
    }
    void Connect(object sender,EventArgs e) {
        try {
            if(updateBusy)throw new Exception("Güncelleme işlemini bekleyin.");
            using(WindowsIdentity id=WindowsIdentity.GetCurrent())if(!new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator))throw new Exception("Uygulamayı yönetici olarak çalıştırın.");
            if(proxies.SelectedItem==null)throw new Exception("Proxy seçin.");
            if(selectedFiles.Length==0||!File.Exists(launcherPath))throw new Exception("Client ve launcher dosyalarını seçin.");
            string bin=Path.Combine(baseDir,"sing-box.exe"); 
            configFile=Path.Combine(Path.GetTempPath(),"metinproxy-"+Guid.NewGuid().ToString("N")+".json");
            File.WriteAllText(configFile,Config(launcherPath,(ProxyItem)proxies.SelectedItem),new UTF8Encoding(false));
            starting=true;ready=false;SetBusy(true);status.Text="Durum: Yapılandırma kontrol ediliyor…";
            var worker=new Thread(delegate() {StartEngine(bin);}); worker.IsBackground=true;worker.Start();
        }catch(Exception ex){CleanConfig();Error(ex);}
    }
    ProcessStartInfo Info(string bin,string command) {
        return new ProcessStartInfo {FileName=bin,Arguments=command+" -c \""+configFile+"\"",WorkingDirectory=Path.GetDirectoryName(bin),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
    }
    [System.Runtime.InteropServices.DllImport("dnsapi.dll")] static extern bool DnsFlushResolverCache();
    static void FlushDns() {DnsFlushResolverCache();}
    void StartEngine(string bin) {
        try {
            bin=EngineBootstrap.Ensure(bin,delegate(string text){UI(delegate {Log(text);});});
            using(Process check=new Process {StartInfo=Info(bin,"check")}) {
                var output=new StringBuilder();
                check.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data!=null)lock(output)output.AppendLine(e.Data);};
                check.ErrorDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data!=null)lock(output)output.AppendLine(e.Data);};
                check.Start();check.BeginOutputReadLine();check.BeginErrorReadLine();
                if(!check.WaitForExit(15000)){check.Kill();check.WaitForExit();throw new Exception("Motor kontrolü zaman aşımına uğradı.");}
                check.WaitForExit();if(check.ExitCode!=0)throw new Exception("Yapılandırma reddedildi: "+output.ToString());
            }
            Process proc=new Process {StartInfo=Info(bin,"run")};
            DataReceivedEventHandler handler=delegate(object s,DataReceivedEventArgs e) {
                if(e.Data==null)return;
                string line=e.Data; UI(delegate {line=Regex.Replace(line,"\\x1B\\[[0-?]*[ -/]*[@-~]","");Log(line);
                if(line.IndexOf("failed to search process",StringComparison.OrdinalIgnoreCase)>=0)status.Text="UYARI: Süreç okunamadı; oyun proxy kullanımı doğrulanmadı.";
                if(line.IndexOf("outbound/socks[game-proxy]",StringComparison.OrdinalIgnoreCase)>=0)status.Text="Proxy yönlendirme denemesi görüldü; oyun bağlantısı henüz doğrulanmadı.";
                if(line.Contains("sing-box started")){ready=true;launch.Enabled=true;status.Text="Durum: Motor hazır — oyun bağlantısı henüz doğrulanmadı";}});
            };
            proc.OutputDataReceived+=handler;proc.ErrorDataReceived+=handler;
            FlushDns();
            proc.Start();engine=proc;
            UI(delegate {starting=false;SetBusy(true);status.Text="Durum: Motor başlatılıyor…";});
            proc.BeginOutputReadLine();proc.BeginErrorReadLine();
            proc.WaitForExit();int exit=proc.ExitCode;proc.Dispose();engine=null;
            UI(delegate {Log("Motor kapandı (kod "+exit+"). Oyun açık kalırsa normal IP ile bağlanabilir.");Finished();});
        }catch(Exception ex){UI(delegate {Log(ex.Message);Finished();});}
    }
    void Finished() {starting=false;ready=false;CleanConfig();SetBusy(false);status.Text="Durum: Bağlı değil";}
    void Stop(object sender,EventArgs e) {
        // sing-box's Clash API is not enabled; killing is explicit and requires route cleanup verification.
        try {if(engine!=null&&!engine.HasExited){engine.Kill();status.Text="Durum: Motor durduruluyor…";}}catch(Exception ex){Error(ex);}
    }
    void Launch(object sender,EventArgs e) {
        try {
            if(!ready || engine==null || engine.HasExited)throw new Exception("Motor hazır değil.");
            Process.Start(new ProcessStartInfo {FileName=launcherPath,WorkingDirectory=Path.GetDirectoryName(launcherPath),UseShellExecute=true});
            Log("Client açıldı. Oyun bağlantılarında game-proxy çıkışını doğrulayın.");
        }catch(Exception ex){Error(ex);}
    }
    void CleanConfig() {if(configFile!=null)try {File.Delete(configFile);configFile=null;}catch(IOException){}catch(UnauthorizedAccessException){}}
    void OnClosing(object sender,FormClosingEventArgs e) {
        if(starting || updateBusy){e.Cancel=true;MessageBox.Show(this,"Motor başlatılıyor. Tamamlandıktan sonra kapatın.");return;}
        closing=true;
        try {if(engine!=null&&!engine.HasExited){engine.Kill();engine.WaitForExit(5000);}}catch(Exception){}
        CleanConfig();
    }
    void OpenLogs(object sender,EventArgs e) {
        try {
            if(localLogPath==null) {
                localLogPath=Diagnostics.CreateSession();
                var profiles=new List<ProxyItem>();foreach(ProxyItem p in proxies.Items)profiles.Add(p);
                Diagnostics.Append(localLogPath,logs.Text,profiles);
            }
            Process.Start(new ProcessStartInfo{FileName=Path.GetDirectoryName(localLogPath),UseShellExecute=true});
        }catch(Exception ex){Error(ex);}
    }
    void CheckUpdates(object sender,EventArgs e) {
        if(updateBusy || starting || engine!=null)return;
        if(availableUpdate!=null) {InstallUpdate();return;}
        updateBusy=true;update.Enabled=false;connect.Enabled=false;
        var thread=new Thread(delegate() {
            try {
                UpdateRelease result=Updates.Check();
                UI(delegate {availableUpdate=result;Log(result==null?"Sürüm güncel; yeni yayın bulunamadı.":"Yeni sürüm: "+result.Version);UpdateFinished();});
            }catch(Exception ex){UI(delegate {Log("Güncelleme kontrolü: "+ex.Message);UpdateFinished();});}
        });thread.IsBackground=true;thread.Start();
    }
    void UpdateFinished() {
        updateBusy=false;update.Enabled=true;connect.Enabled=true;
        update.Text=availableUpdate==null?"Güncelleme kontrol":"Güncelle: v"+availableUpdate.Version;
    }
    void InstallUpdate() {
        if(MessageBox.Show(this,"Yeni sürüm indirilecek ve uygulama yeniden başlayacak. Oyunlarınızı önce kapatın.","NiyazisugarProxy",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
        updateBusy=true;update.Enabled=false;connect.Enabled=false;update.Text="İndiriliyor…";
        var thread=new Thread(delegate() {
            try {
                string staged=Updates.Stage(availableUpdate);
                UI(delegate {try {Updates.RunInstaller(staged,availableUpdate.Digest);updateBusy=false;Close();}catch(Exception ex){Log(ex.Message);UpdateFinished();}});
            }catch(Exception ex){UI(delegate {Log("Güncelleme: "+ex.Message);UpdateFinished();});}
        });thread.IsBackground=true;thread.Start();
    }
    [STAThread] static void Main(string[] args) {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length>0 && args[0]=="--apply-update") {Updates.Apply(args);return;}
        Application.Run(new MainForm());
    }
}
