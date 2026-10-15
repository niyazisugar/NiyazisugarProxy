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

partial class MainForm : Form {
    class ClientSlot {
        public ComboBox Proxy=new PremiumComboBox(),Server=new PremiumComboBox();
        public TextBox Client=new TextBox(),IP=new TextBox();
        public Button Select,Launch,Save,Connect,Stop,Add;public ClientRoute Active;
        public string Launcher;public string[] Files=new string[0];public Panel Card;
    }
    readonly ClientSlot[] slots={new ClientSlot()};
    ComboBox proxies{get{return slots[0].Proxy;}}
    TextBox client{get{return slots[0].Client;}} TextBox serverIP{get{return slots[0].IP;}}
    string launcherPath{get{return slots[0].Launcher;}} string[] selectedFiles{get{return slots[0].Files;}}
    Label status=new Label();Button load,add,connect,stop,update,sendReport,minimize,close;
    ComboBox theme=new PremiumComboBox();AppPreferences preferences;
    UpdateRelease availableUpdate;bool updateBusy,updateFailed;string localLogPath;
    readonly EngineLogBuffer engineLogs=new EngineLogBuffer();System.Windows.Forms.Timer logTimer;DateTime nextPerformance=DateTime.UtcNow.AddMinutes(1);
    SlotRelay[] relays;LiveRouting liveRoutes;Process engine;string configFile;
    bool starting,closing,ready,initializing=true,previewMode,proxyLoading;
    int savedSecondProxy=-1;
    readonly JavaScriptSerializer json=new JavaScriptSerializer();readonly string baseDir=AppDomain.CurrentDomain.BaseDirectory;
    public MainForm():this(false){}
    public MainForm(bool preview){
        previewMode=preview;preferences=AppPreferences.Load();InitializeDesign();
        RefreshServers();var saved=preferences.Slots[0];slots[0].Launcher=saved.Launcher;slots[0].Files=saved.Files??new string[0];client.Text=String.Join(" ; ",slots[0].Files);serverIP.Text=saved.IP??"";
        foreach(var profile in preferences.Servers)if(profile.IP==serverIP.Text){slots[0].Server.SelectedItem=profile;break;}
        if(!preview)RestoreProxies();proxies.SelectedIndexChanged+=delegate{SaveProxies();UpdateConnectionDetails();};
        initializing=false;UpdateGameFile();ApplyTheme();ShowPage(0);SetBusy(false);RefreshConnectionStatus();
        Resize+=delegate{if(!initializing&&WindowState!=FormWindowState.Minimized){LayoutWindow();RoundWindow();}};ResizeEnd+=delegate{Persist();};LayoutWindow();RoundWindow();FormClosing+=OnClosing;
        if(!preview){logTimer=new System.Windows.Forms.Timer{Interval=250};logTimer.Tick+=delegate{DrainEngineLogs();RefreshLogView(false);if(DateTime.UtcNow>=nextPerformance){nextPerformance=DateTime.UtcNow.AddMinutes(1);RecordPerformance();}};logTimer.Start();
            try{localLogPath=Diagnostics.CreateSession();Log("Uygulama başlatıldı.");}catch(Exception ex){AppendLogEntries("Log klasörü oluşturulamadı: "+ex.Message);}
            Shown+=delegate{CheckUpdates(null,EventArgs.Empty);};}
    }
    Button MakeButton(string text,Control parent,int x,int y,int width,EventHandler action){var button=new PremiumButton{Text=text};button.SetBounds(x,y,width,36);button.Click+=action;parent.Controls.Add(button);return button;}
    void AddLabel(Control parent,string text,int x,int y,int size,bool bold){parent.Controls.Add(new Label{Text=text,AutoSize=true,ForeColor=Palette.Text,BackColor=Color.Transparent,Left=x,Top=y,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular)});}
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ReleaseCapture();
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,int wparam,int lparam);
    void DragWindow(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,2,0);}}
    protected override void WndProc(ref Message message){
        if(message.Msg==0x02E0&&!initializing){long dpi=message.WParam.ToInt64()&0xffff;uiDpi=Math.Max(1,dpi/96f);var proposed=(NativeRect)System.Runtime.InteropServices.Marshal.PtrToStructure(message.LParam,typeof(NativeRect));var rectangle=Rectangle.FromLTRB(proposed.Left,proposed.Top,proposed.Right,proposed.Bottom);UpdateMinimumSize(Screen.FromRectangle(rectangle).WorkingArea.Size);var work=Screen.FromRectangle(rectangle).WorkingArea;var fit=WindowGeometry.Restore(rectangle.Width,rectangle.Height,work.Size);Bounds=new Rectangle(Math.Max(work.Left,Math.Min(rectangle.Left,work.Right-fit.Width)),Math.Max(work.Top,Math.Min(rectangle.Top,work.Bottom-fit.Height)),fit.Width,fit.Height);LayoutWindow();message.Result=IntPtr.Zero;return;}
        if(message.Msg==0x84&&WindowState==FormWindowState.Normal){base.WndProc(ref message);if(message.Result==new IntPtr(1)){long bits=message.LParam.ToInt64();var point=PointToClient(new Point((short)(bits&0xffff),(short)((bits>>16)&0xffff)));int hit=WindowGeometry.HitTest(point,ClientSize,(int)(8*uiDpi));if(hit!=1)message.Result=new IntPtr(hit);}return;}base.WndProc(ref message);
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
    void RoundWindow(){if(ClientSize.Width<1||ClientSize.Height<1)return;using(var path=RoundedPanel.Shape(ClientRectangle,(int)(12*uiDpi))){var old=Region;Region=new Region(path);if(old!=null)old.Dispose();}}
    void RefreshServers(){for(int i=0;i<slots.Length;i++){string ip=slots[i].IP.Text;slots[i].Server.Items.Clear();slots[i].Server.Items.Add("Kayıtlı sunucu seç");foreach(var profile in preferences.Servers)slots[i].Server.Items.Add(profile);slots[i].Server.SelectedIndex=0;slots[i].IP.Text=ip;}}
    void SaveServer(int index){try{var s=slots[index];string cidr=RoutingConfig.ValidateFallback(s.IP.Text);if(cidr==null)throw new Exception("Önce sunucu IP'sini girin.");using(var dialog=new Form{Text="Sunucuyu kaydet",BackColor=Palette.Background,ForeColor=Palette.Text,Font=Font,ClientSize=new Size(420,130),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){AddLabel(dialog,"Sunucu adı (ör. Sezar2)",15,12,10,false);var name=new TextBox{Left=15,Top=42,Width=390,MaxLength=80,BackColor=Palette.Field,ForeColor=Palette.Text};dialog.Controls.Add(name);var selected=s.Server.SelectedItem as ServerProfile;if(selected!=null)name.Text=selected.Name;var ok=MakeButton("Kaydet",dialog,275,84,130,delegate{});ok.DialogResult=DialogResult.OK;dialog.AcceptButton=ok;if(dialog.ShowDialog(this)!=DialogResult.OK)return;if(String.IsNullOrWhiteSpace(name.Text))throw new Exception("Sunucu adı yazın.");preferences.PutServer(name.Text.Trim(),cidr.Substring(0,cidr.Length-3));RefreshServers();s.Server.SelectedItem=preferences.Servers.Find(delegate(ServerProfile item){return item.Name.Equals(name.Text.Trim(),StringComparison.OrdinalIgnoreCase);});Persist();Log("Sunucu kaydedildi: "+name.Text.Trim());}}catch(Exception ex){Error(ex);}}
    void Persist(){if(initializing||previewMode)return;try{preferences.Light=theme.SelectedIndex==1;preferences.PremiumAccent=designAccent;preferences.SecondEnabled=false;preferences.LogAutoScroll=autoScroll.Checked;preferences.LogLevel=levelFilter.SelectedIndex;if(WindowState==FormWindowState.Normal){preferences.LayoutRevision=1;preferences.WindowWidth=ClientSize.Width;preferences.WindowHeight=ClientSize.Height;}preferences.Slots[0]=new SlotPreference{Launcher=slots[0].Launcher,Files=slots[0].Files,IP=serverIP.Text};preferences.Save();}catch(Exception ex){Log("Ayarlar kaydedilemedi: "+ex.Message);}}
    void UI(Action a) { if(IsDisposed || closing)return; try { BeginInvoke(a); } catch(InvalidOperationException){} }
    void RecordPerformance(){try{using(var current=Process.GetCurrentProcess()){current.Refresh();Log("Performans "+DateTime.Now.ToString("O")+" | uygulama: "+Performance(current)+" | log kuyruğu: "+engineLogs.Count);}var motor=engine;if(motor!=null&&!motor.HasExited){motor.Refresh();Log("Performans motor: "+Performance(motor));}}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}
    string Performance(Process process){return "PID="+process.Id+", CPU toplam="+process.TotalProcessorTime.TotalSeconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" sn, bellek="+(process.WorkingSet64/1048576)+" MB, iş parçacığı="+process.Threads.Count;}
    void DrainEngineLogs(){string text=engineLogs.Drain();if(String.IsNullOrEmpty(text))return;text=Regex.Replace(text,"\\x1B\\[[0-?]*[ -/]*[@-~]","");Log(text);if(text.IndexOf("failed to search process",StringComparison.OrdinalIgnoreCase)>=0)status.Text="UYARI: Süreç okunamadı; oyun proxy kullanımı doğrulanmadı.";if(text.Contains("sing-box started")){ready=true;SetBusy(true);status.Text="Durum: Motor hazır — oyun bağlantısı henüz doğrulanmadı";}}
    void Log(string text) {
        if(String.IsNullOrEmpty(text))return;
        foreach(ProxyItem p in AllProxies()) {
            if(!String.IsNullOrEmpty(p.Password))text=text.Replace(p.Password,"[gizli]");
            if(!String.IsNullOrEmpty(p.User))text=text.Replace(p.User,"[gizli]");
        }
        if(localLogPath!=null) {
            try {
                var profiles=new List<ProxyItem>();foreach(ProxyItem p in AllProxies())profiles.Add(p);
                if(new FileInfo(localLogPath).Length>10*1024*1024)localLogPath=Diagnostics.CreateSession();
                Diagnostics.Append(localLogPath,text,profiles);
            }catch(Exception ex){localLogPath=null;AppendLogEntries("Log dosyasına yazılamadı: "+ex.Message+Environment.NewLine);}
        }
        AppendLogEntries(text);
    }
    void Error(Exception e) {MessageBox.Show(this,e.Message,"NiyaziSUGAR Proxy",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    void RestoreProxies(){try{var saved=ProxyVault.Load();proxyLoading=true;savedSecondProxy=saved.Selected[1];for(int i=0;i<slots.Length;i++){slots[i].Proxy.Items.AddRange(saved.Items);int selected=saved.Selected[i];slots[i].Proxy.SelectedIndex=saved.Items.Length==0?-1:selected>=0&&selected<saved.Items.Length?selected:Math.Min(i,saved.Items.Length-1);}}catch(Exception ex){AppendLogEntries("Kayıtlı proxy listesi okunamadı; kayıt dosyası korundu. "+ex.GetType().Name+Environment.NewLine);}finally{proxyLoading=false;}}
    void SaveProxies(){if(initializing||previewMode||proxyLoading)return;try{var items=new List<ProxyItem>();foreach(ProxyItem item in proxies.Items)items.Add(item);ProxyVault.Save(new ProxySnapshot{Items=items.ToArray(),Selected=new int[]{slots[0].Proxy.SelectedIndex,savedSecondProxy}});}catch(Exception ex){Log("Proxy listesi kaydedilemedi: "+ex.GetType().Name);}}
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
                proxyLoading=true;try{for(int i=0;i<slots.Length;i++){slots[i].Proxy.Items.Clear();slots[i].Proxy.Items.AddRange(list.ToArray());slots[i].Proxy.SelectedIndex=Math.Min(i,list.Count-1);}}finally{proxyLoading=false;}SaveProxies(); Log(list.Count+" proxy yüklendi.");
            } catch(Exception ex) {Error(ex);}
        }
    }
    void AddProxy(object sender,EventArgs e) {
        using(Form d=new Form {Text="Proxy ekle",BackColor=Palette.Background,ForeColor=Palette.Text,Font=Font,ClientSize=new Size(480,125),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
            var label=new Label {ForeColor=Palette.Text,Text="IPv4:PORT veya IPv4:PORT:KULLANICI:ŞİFRE",AutoSize=true,Location=new Point(12,12)};
            var input=new TextBox {Location=new Point(12,40),Width=450,UseSystemPasswordChar=true,BackColor=Palette.Field,ForeColor=Palette.Text};
            var ok=new PremiumButton {Text="Ekle",Kind="primary",Location=new Point(332,78),Size=new Size(130,36),DialogResult=DialogResult.OK};
            d.Controls.Add(label);d.Controls.Add(input);d.Controls.Add(ok);d.AcceptButton=ok;
            if(d.ShowDialog(this)==DialogResult.OK)try {var item=ProxyItem.Parse(input.Text);proxyLoading=true;try{for(int i=0;i<slots.Length;i++){slots[i].Proxy.Items.Add(item);if(slots[i].Active==null&&(i==0||slots[i].Proxy.SelectedIndex<0))slots[i].Proxy.SelectedIndex=slots[i].Proxy.Items.Count-1;}}finally{proxyLoading=false;}SaveProxies();}catch(Exception ex){Error(ex);}
        }
    }
    void SelectSlot(int index) {
        var slot=slots[index];
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
                slot.Files=d.FileNames;slot.Launcher=launcher;slot.Client.Text=String.Join(" ; ",d.FileNames);UpdateGameFile();Persist();
                Log("Client "+(index+1)+" launcher: "+slot.Launcher);
                Log("Yönlendirilecek client sayısı: "+patterns.Length);
            }catch(Exception ex){Error(ex);}
        }
    }
    IEnumerable<ProxyItem> AllProxies(){var seen=new HashSet<ProxyItem>();foreach(var slot in slots)foreach(ProxyItem item in slot.Proxy.Items)if(seen.Add(item))yield return item;}
    void SetBusy(bool busy){bool editable=!starting&&slots[0].Active==null&&!updateBusy;load.Enabled=add.Enabled=slots[0].Select.Enabled=proxies.Enabled=slots[0].Server.Enabled=serverIP.Enabled=slots[0].Save.Enabled=editable;connect.Enabled=editable;slots[0].Launch.Enabled=ready&&slots[0].Active!=null&&engine!=null;stop.Enabled=!starting&&engine!=null;update.Enabled=!busy&&!updateBusy;RefreshConnectionStatus();}
    ClientRoute ReadSlot(int index){var slot=slots[index];var proxy=slot.Proxy.SelectedItem as ProxyItem;if(proxy==null)throw new Exception("Client "+(index+1)+" için proxy seçin.");if(slot.Files.Length==0||!File.Exists(slot.Launcher))throw new Exception("Client "+(index+1)+" için client ve launcher dosyalarını seçin.");RoutingConfig.ValidateFallback(slot.IP.Text);return new ClientRoute{Proxy=proxy,Patterns=ClientFiles.Patterns(slot.Files,slot.Launcher),IP=slot.IP.Text,Tag="game-proxy-"+(index+1)};}
    void Connect(object sender,EventArgs e){ActivateSlot(0);}
    void ActivateSlot(int index){
        if(starting||slots[index].Active!=null)return;
        try{
            if(updateBusy)throw new Exception("Güncelleme işlemini bekleyin.");
            using(WindowsIdentity id=WindowsIdentity.GetCurrent())if(!new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator))throw new Exception("Uygulamayı yönetici olarak çalıştırın.");
            var candidate=ReadSlot(index);
            bool firstEngine=engine==null;
            if(firstEngine){liveRoutes=new LiveRouting();relays=new SlotRelay[]{new SlotRelay(),new SlotRelay()};}
            relays[index].SetProxy(candidate.Proxy);slots[index].Active=candidate;
            liveRoutes.Update(new ClientRoute[]{slots[0].Active,null});Persist();
            if(firstEngine){var adapter=NetworkRoutes.Select();configFile=Path.Combine(Path.GetTempPath(),"metinproxy-"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(configFile,json.Serialize(liveRoutes.Config(new int[]{relays[0].Port,relays[1].Port},adapter.alias)),new UTF8Encoding(false));starting=true;ready=false;SetBusy(true);status.Text="Motor başlatılıyor…";var worker=new Thread(delegate(){StartEngine(Path.Combine(baseDir,"sing-box.exe"));});worker.IsBackground=true;worker.Start();}
            else{SetBusy(true);Log("Client "+(index+1)+" kuralları güncellendi; motor yeniden başlatılmadı. Yeni kuralların uygulanması birkaç saniye sürebilir.");}
        }catch(Exception ex){if(relays!=null)relays[index].Stop();slots[index].Active=null;if(engine==null){DisposeLive();CleanConfig();}else try{liveRoutes.Update(new ClientRoute[]{slots[0].Active,null});}catch(Exception updateError){Log("Kurallar geri yüklenemedi: "+updateError.Message);}SetBusy(starting||engine!=null);Error(ex);}
    }
    void StopSlot(int index){if(starting||slots[index].Active==null)return;try{relays[index].Stop();slots[index].Active=null;liveRoutes.Update(new ClientRoute[]{slots[0].Active,null});Log("Client "+(index+1)+" proxy bağlantıları durduruldu; diğer client değişmedi.");SetBusy(engine!=null);}catch(Exception ex){Error(ex);}}
    void DisposeLive(){if(relays!=null){foreach(var relay in relays)if(relay!=null)relay.Dispose();relays=null;}if(liveRoutes!=null){liveRoutes.Dispose();liveRoutes=null;}foreach(var slot in slots)slot.Active=null;}
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
                engineLogs.Add(e.Data);
            };
            proc.OutputDataReceived+=handler;proc.ErrorDataReceived+=handler;
            FlushDns();
            proc.Start();engine=proc;
            UI(delegate {starting=false;SetBusy(true);status.Text="Motor başlatılıyor…";});
            proc.BeginOutputReadLine();proc.BeginErrorReadLine();
            proc.WaitForExit();int exit=proc.ExitCode;proc.Dispose();engine=null;
            UI(delegate {Log("Motor kapandı (kod "+exit+"). Oyun açık kalırsa normal IP ile bağlanabilir.");Finished();});
        }catch(Exception ex){UI(delegate {Log(ex.Message);Finished();});}
    }
    void Finished() {DrainEngineLogs();engineLogs.Clear();starting=false;ready=false;DisposeLive();CleanConfig();SetBusy(false);status.Text="Durum: Bağlı değil";}
    void Stop(object sender,EventArgs e) {
        // sing-box's Clash API is not enabled; killing is explicit and requires route cleanup verification.
        try {if(engine!=null&&!engine.HasExited){engine.Kill();status.Text="Durum: Motor durduruluyor…";}}catch(Exception ex){Error(ex);}
    }
    void LaunchSlot(int index) {
        var slot=slots[index];
        try {
            if(slot.Active==null)throw new Exception("Önce bu clientin proxy bağlantısını başlatın.");
            if(!ready || engine==null || engine.HasExited)throw new Exception("Motor hazır değil.");
            Process.Start(new ProcessStartInfo {FileName=slot.Launcher,WorkingDirectory=Path.GetDirectoryName(slot.Launcher),UseShellExecute=true});
            Log("Client "+(index+1)+" açıldı. Oyun bağlantılarında game-proxy-"+(index+1)+" çıkışını doğrulayın.");
        }catch(Exception ex){Error(ex);}
    }
    void CleanConfig() {if(configFile!=null)try {File.Delete(configFile);configFile=null;}catch(IOException){}catch(UnauthorizedAccessException){}}
    void OnClosing(object sender,FormClosingEventArgs e) {
        if(starting || updateBusy){e.Cancel=true;MessageBox.Show(this,"Motor başlatılıyor. Tamamlandıktan sonra kapatın.");return;}
        Persist();closing=true;
        try {if(engine!=null&&!engine.HasExited){engine.Kill();engine.WaitForExit(5000);}}catch(Exception){}
        if(logTimer!=null){logTimer.Stop();DrainEngineLogs();logTimer.Dispose();}DisposeLive();CleanConfig();
    }
    void OpenLogs(object sender,EventArgs e) {
        try {
            if(localLogPath==null) {
                localLogPath=Diagnostics.CreateSession();
                var profiles=new List<ProxyItem>();foreach(ProxyItem p in AllProxies())profiles.Add(p);
                Diagnostics.Append(localLogPath,logBook.AllText(),profiles);
            }
            Process.Start(new ProcessStartInfo{FileName=Path.GetDirectoryName(localLogPath),UseShellExecute=true});
        }catch(Exception ex){Error(ex);}
    }
    void CheckUpdates(object sender,EventArgs e) {
        if(updateBusy || starting || engine!=null)return;
        if(availableUpdate!=null) {InstallUpdate();return;}
        updateFailed=false;updateBusy=true;update.Enabled=false;connect.Enabled=false;
        var thread=new Thread(delegate() {
            try {
                UpdateRelease result=Updates.Check();
                UI(delegate {availableUpdate=result;Log(result==null?"Sürüm güncel; yeni yayın bulunamadı.":"Yeni sürüm: "+result.Version);UpdateFinished();});
            }catch(Exception ex){UI(delegate {updateFailed=true;Log("Güncelleme kontrolü: "+ex.Message);UpdateFinished();});}
        });thread.IsBackground=true;thread.Start();
    }
    void UpdateFinished() {
        updateBusy=false;SetBusy(starting||engine!=null);
        updateHint.Text=updateFailed?"Güncelleme kontrolü şu anda kullanılamıyor.":availableUpdate==null?"Yüklü sürüm güncel.":"Yeni sürüm indirilmeye hazır.";
        update.Text=availableUpdate==null?"Güncellemeleri Kontrol Et":"Güncelle: v"+availableUpdate.Version;
    }
    void InstallUpdate() {
        if(MessageBox.Show(this,"Yeni sürüm indirilecek ve uygulama yeniden başlayacak. Oyunlarınızı önce kapatın.","NiyaziSUGAR Proxy",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
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

