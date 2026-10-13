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
    class ClientSlot {
        public ComboBox Proxy=new ComboBox(), Server=new ComboBox();
        public TextBox Client=new TextBox(), IP=new TextBox();
        public Button Select, Launch, Save;
        public string Launcher; public string[] Files=new string[0];
        public Panel Card;
    }
    readonly ClientSlot[] slots={new ClientSlot(),new ClientSlot()};
    ComboBox proxies {get{return slots[0].Proxy;}}
    TextBox client {get{return slots[0].Client;}}
    TextBox serverIP {get{return slots[0].IP;}}
    string launcherPath {get{return slots[0].Launcher;}}
    string[] selectedFiles {get{return slots[0].Files;}}
    TextBox logs=new TextBox(); Label status=new Label();
    Button load,add,connect,stop,update,sendReport;
    CheckBox second=new CheckBox(); ComboBox theme=new ComboBox(),accent=new ComboBox();
    Panel tabs=new Panel(); Panel[] pages;Button[] navigation=new Button[3];int selectedPage; AppPreferences preferences;
    UpdateRelease availableUpdate; bool updateBusy; string localLogPath;
    Process engine; string configFile; bool starting,closing,ready,initializing=true,previewMode;
    readonly JavaScriptSerializer json=new JavaScriptSerializer();
    readonly string baseDir=AppDomain.CurrentDomain.BaseDirectory;
    public MainForm() : this(false) {}
    public MainForm(bool preview) {
        previewMode=preview;preferences=AppPreferences.Load();
        Text="NiyazisugarProxy"; FormBorderStyle=FormBorderStyle.None;ClientSize=new Size(1000,780); MinimumSize=MaximumSize=Size;
         StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10); DoubleBuffered=true;
        var header=new Panel{Dock=DockStyle.Top,Height=88};Controls.Add(header);
        header.Controls.Add(new Label{Text="NIYAZISUGAR",Font=new Font("Segoe UI",21,FontStyle.Bold),AutoSize=true,Location=new Point(26,14)});
        header.Controls.Add(new Label{Text="PROXY  /  v"+Updates.Current.ToString(3),AutoSize=true,Location=new Point(29,53)});
        header.MouseDown+=DragWindow;foreach(Control c in header.Controls)c.MouseDown+=DragWindow;
        var minimize=MakeButton("—",header,890,20,40,delegate{WindowState=FormWindowState.Minimized;});
        var close=MakeButton("×",header,936,20,40,delegate{Close();});minimize.TabStop=false;close.TabStop=false;
        tabs.SetBounds(22,145,956,613);tabs.Font=Font;Controls.Add(tabs);
        var dashboard=new Panel{Dock=DockStyle.Fill};var settings=new Panel{Dock=DockStyle.Fill};var activity=new Panel{Dock=DockStyle.Fill};
        pages=new Panel[]{dashboard,settings,activity};tabs.Controls.AddRange(pages);
        for(int nav=0;nav<3;nav++){int page=nav;navigation[nav]=MakeButton(new string[]{"Clientler","Ayarlar","Loglar"}[nav],this,22+nav*155,99,145,delegate{ShowPage(page);});}
        load=MakeButton("Liste yükle",dashboard,18,18,130,LoadList);
        add=MakeButton("Proxy ekle",dashboard,158,18,130,AddProxy);
        second.Text="2. client etkin";second.SetBounds(730,23,185,30);second.Checked=preferences.SecondEnabled;dashboard.Controls.Add(second);
        second.CheckedChanged+=delegate{SetSecondState();Persist();};
        for(int i=0;i<2;i++)BuildSlot(dashboard,i);
        SetSecondState();
        connect=MakeButton("Proxyleri bağla",dashboard,18,496,210,Connect);
        stop=MakeButton("Durdur",dashboard,240,496,120,Stop);stop.Enabled=false;
        status.SetBounds(18,551,904,42);status.Text="Hazır • Client ve proxy seçerek başlayın.";dashboard.Controls.Add(status);
        var appearance=new RoundedPanel();appearance.SetBounds(18,18,904,228);settings.Controls.Add(appearance);
        AddLabel(appearance,"Görünüm",20,15,16,true);
        AddLabel(appearance,"Tema",20,65,10,false);theme.SetBounds(210,61,300,30);theme.DropDownStyle=ComboBoxStyle.DropDownList;
        theme.Items.AddRange(new object[]{"Karanlık","Açık"});theme.SelectedIndex=preferences.Light?1:0;appearance.Controls.Add(theme);
        AddLabel(appearance,"Vurgu rengi",20,113,10,false);accent.SetBounds(210,109,300,30);accent.DropDownStyle=ComboBoxStyle.DropDownList;
        accent.Items.AddRange(new object[]{"Mor","Mavi","Yeşil"});accent.SelectedIndex=Math.Max(0,Math.Min(2,preferences.Accent));appearance.Controls.Add(accent);
        AddLabel(appearance,"Tema ve sunucu kayıtları bu bilgisayarda otomatik saklanır.",20,174,10,false);
        theme.SelectedIndexChanged+=delegate{ApplyTheme();Persist();};accent.SelectedIndexChanged+=delegate{ApplyTheme();Persist();};
        var maintenance=new RoundedPanel();maintenance.SetBounds(18,267,904,185);settings.Controls.Add(maintenance);
        AddLabel(maintenance,"Uygulama",20,16,16,true);
        update=MakeButton("Güncelleme kontrol",maintenance,20,65,270,CheckUpdates);
        MakeButton("Log klasörünü aç",maintenance,305,65,220,OpenLogs);
        AddLabel(maintenance,"Loglar masaüstündeki NiyazisugarProxy-Loglar klasörüne kaydedilir.",20,125,10,false);
        logs.SetBounds(18,18,904,518);logs.Multiline=true;logs.ReadOnly=true;logs.ScrollBars=ScrollBars.Vertical;logs.Font=new Font("Consolas",9);activity.Controls.Add(logs);
        sendReport=MakeButton("Log klasörünü aç",activity,18,552,220,OpenLogs);
        AddLabel(activity,"Proxy kullanıcı adları ve şifreleri kayıtta gizlenir.",260,563,10,false);
        RefreshServers();
        for(int i=0;i<2;i++) {
            SlotPreference saved=preferences.Slots[i];slots[i].Launcher=saved.Launcher;slots[i].Files=saved.Files??new string[0];
            slots[i].Client.Text=String.Join(" ; ",slots[i].Files);slots[i].IP.Text=saved.IP??"";
            foreach(ServerProfile profile in preferences.Servers)if(profile.IP==slots[i].IP.Text){slots[i].Server.SelectedItem=profile;break;}
        }
        initializing=false;PrepareFields(this);ShowPage(0);ApplyTheme();Resize+=delegate{RoundWindow();};RoundWindow();
        FormClosing+=OnClosing;
        if(!preview) {
            try{localLogPath=Diagnostics.CreateSession();Log("Yerel log kaydı başladı.");}catch(Exception ex){logs.AppendText("Log klasörü oluşturulamadı: "+ex.Message+Environment.NewLine);}
            Shown+=delegate{CheckUpdates(null,EventArgs.Empty);};
        }
    }
    void BuildSlot(Control parent,int index) {
        var s=slots[index];s.Card=new RoundedPanel();s.Card.SetBounds(18,74+index*205,904,190);parent.Controls.Add(s.Card);
        AddLabel(s.Card,"CLIENT "+(index+1),18,13,14,true);
        s.Launch=MakeButton("Oyunu aç",s.Card,746,10,140,delegate(object a,EventArgs b){LaunchSlot(index);});s.Launch.Enabled=false;
        AddLabel(s.Card,"Proxy",18,56,9,false);s.Proxy.SetBounds(88,52,322,30);s.Proxy.DropDownStyle=ComboBoxStyle.DropDownList;s.Card.Controls.Add(s.Proxy);
        AddLabel(s.Card,"Sunucu",436,56,9,false);s.Server.SetBounds(510,52,376,30);s.Server.DropDownStyle=ComboBoxStyle.DropDownList;s.Card.Controls.Add(s.Server);
        s.Server.SelectedIndexChanged+=delegate{var profile=s.Server.SelectedItem as ServerProfile;if(profile!=null){s.IP.Text=profile.IP;Persist();}};
        s.Client.SetBounds(18,98,718,28);s.Client.ReadOnly=true;s.Card.Controls.Add(s.Client);
        s.Select=MakeButton("Client seç",s.Card,746,94,140,delegate(object a,EventArgs b){SelectSlot(index);});
        AddLabel(s.Card,"Sunucu IP",18,149,9,false);s.IP.SetBounds(110,143,250,28);s.Card.Controls.Add(s.IP);
        s.IP.TextChanged+=delegate{Persist();};s.Save=MakeButton("IP'yi kaydet",s.Card,374,138,170,delegate(object a,EventArgs b){SaveServer(index);});
        AddLabel(s.Card,"EXE + BIN seçimi desteklenir",567,149,9,false);
    }
    void AddLabel(Control parent,string text,int x,int y,int size,bool bold){parent.Controls.Add(new Label{Text=text,AutoSize=true,Location=new Point(x,y),Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular)});}
    Button MakeButton(string text,Control parent,int x,int y,int width,EventHandler action) {
        var b=new RoundedButton{Text=text,Location=new Point(x,y),Size=new Size(width,36),FlatStyle=FlatStyle.Flat};b.FlatAppearance.BorderSize=0;b.Click+=action;parent.Controls.Add(b);return b;
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ReleaseCapture();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr handle,int msg,int wparam,int lparam);
    void DragWindow(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,2,0);}}
    void RoundWindow(){using(var shape=RoundedPanel.Shape(ClientRectangle,20)){var old=Region;Region=new Region(shape);if(old!=null)old.Dispose();}}
    void ApplyTheme(){if(theme.SelectedIndex<0)return;bool light=theme.SelectedIndex==1;Color background=light?Color.FromArgb(239,241,247):Color.FromArgb(15,18,27);Color card=light?Color.White:Color.FromArgb(25,30,43);Color field=light?Color.FromArgb(231,235,244):Color.FromArgb(35,42,59);Color foreground=light?Color.FromArgb(30,36,50):Color.FromArgb(229,235,247);Color tint=accent.SelectedIndex==1?Color.FromArgb(50,120,223):accent.SelectedIndex==2?Color.FromArgb(30,148,111):Color.FromArgb(115,81,217);ThemeControl(this,background,card,field,foreground,tint);for(int i=0;i<navigation.Length;i++){navigation[i].BackColor=i==selectedPage?tint:field;navigation[i].ForeColor=i==selectedPage?Color.White:foreground;}Invalidate(true);}
    void ThemeControl(Control root,Color background,Color card,Color field,Color foreground,Color tint){root.ForeColor=root is Button?Color.White:foreground;root.BackColor=root is RoundedPanel?card:root is Button?tint:root is TextBox||root is ComboBox?field:background;var panel=root as RoundedPanel;if(panel!=null){panel.Surface=card;panel.Border=field;}foreach(Control child in root.Controls)ThemeControl(child,root is RoundedPanel?card:background,card,field,foreground,tint);}
    void ShowPage(int index){selectedPage=index;for(int i=0;i<pages.Length;i++)pages[i].Visible=i==index;ApplyTheme();}
    void SetSecondState(){var s=slots[1];bool enabled=second.Checked;s.Select.Enabled=s.Proxy.Enabled=s.Server.Enabled=s.IP.Enabled=s.Save.Enabled=enabled;s.Launch.Enabled=enabled&&ready;}
    void PrepareFields(Control root){foreach(Control child in root.Controls){var input=child as TextBox;if(input!=null)input.BorderStyle=BorderStyle.None;var combo=child as ComboBox;if(combo!=null){combo.FlatStyle=FlatStyle.Flat;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=23;combo.DrawItem+=delegate(object sender,DrawItemEventArgs e){var box=(ComboBox)sender;using(var brush=new SolidBrush(box.BackColor))e.Graphics.FillRectangle(brush,e.Bounds);string text=e.Index>=0?box.Items[e.Index].ToString():box.Text;TextRenderer.DrawText(e.Graphics,text,box.Font,e.Bounds,box.ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);if((e.State&DrawItemState.Selected)!=0)e.DrawFocusRectangle();};}PrepareFields(child);}}
    void RefreshServers(){for(int i=0;i<2;i++){string ip=slots[i].IP.Text;slots[i].Server.Items.Clear();slots[i].Server.Items.Add("Kayıtlı sunucu seç");foreach(var profile in preferences.Servers)slots[i].Server.Items.Add(profile);slots[i].Server.SelectedIndex=0;slots[i].IP.Text=ip;}}
    void SaveServer(int index){try{var s=slots[index];string cidr=RoutingConfig.ValidateFallback(s.IP.Text);if(cidr==null)throw new Exception("Önce sunucu IP'sini girin.");using(var dialog=new Form{Text="Sunucuyu kaydet",ClientSize=new Size(420,130),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){AddLabel(dialog,"Sunucu adı (ör. Sezar2)",15,12,10,false);var name=new TextBox{Left=15,Top=42,Width=390,MaxLength=80};dialog.Controls.Add(name);var selected=s.Server.SelectedItem as ServerProfile;if(selected!=null)name.Text=selected.Name;var ok=MakeButton("Kaydet",dialog,275,84,130,delegate{});ok.DialogResult=DialogResult.OK;dialog.AcceptButton=ok;if(dialog.ShowDialog(this)!=DialogResult.OK)return;if(String.IsNullOrWhiteSpace(name.Text))throw new Exception("Sunucu adı yazın.");preferences.PutServer(name.Text.Trim(),cidr.Substring(0,cidr.Length-3));RefreshServers();s.Server.SelectedItem=preferences.Servers.Find(delegate(ServerProfile item){return item.Name.Equals(name.Text.Trim(),StringComparison.OrdinalIgnoreCase);});Persist();Log("Sunucu kaydedildi: "+name.Text.Trim());}}catch(Exception ex){Error(ex);}}
    void Persist(){if(initializing||previewMode)return;try{preferences.Light=theme.SelectedIndex==1;preferences.Accent=accent.SelectedIndex;preferences.SecondEnabled=second.Checked;for(int i=0;i<2;i++)preferences.Slots[i]=new SlotPreference{Launcher=slots[i].Launcher,Files=slots[i].Files,IP=slots[i].IP.Text};preferences.Save();}catch(Exception ex){Log("Ayarlar kaydedilemedi: "+ex.Message);}}
    void UI(Action a) { if(IsDisposed || closing)return; try { BeginInvoke(a); } catch(InvalidOperationException){} }
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
                for(int i=0;i<slots.Length;i++){slots[i].Proxy.Items.Clear();slots[i].Proxy.Items.AddRange(list.ToArray());slots[i].Proxy.SelectedIndex=Math.Min(i,list.Count-1);} Log(list.Count+" proxy yüklendi.");
            } catch(Exception ex) {Error(ex);}
        }
    }
    void AddProxy(object sender,EventArgs e) {
        using(Form d=new Form {Text="Proxy ekle",ClientSize=new Size(480,125),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
            var label=new Label {Text="IPv4:PORT veya IPv4:PORT:KULLANICI:ŞİFRE",AutoSize=true,Location=new Point(12,12)};
            var input=new TextBox {Location=new Point(12,40),Width=450,UseSystemPasswordChar=true};
            var ok=new Button {Text="Ekle",Location=new Point(362,78),DialogResult=DialogResult.OK};
            d.Controls.Add(label);d.Controls.Add(input);d.Controls.Add(ok);d.AcceptButton=ok;
            if(d.ShowDialog(this)==DialogResult.OK)try {var item=ProxyItem.Parse(input.Text);for(int i=0;i<slots.Length;i++){slots[i].Proxy.Items.Add(item);if(i==0||slots[i].Proxy.SelectedIndex<0)slots[i].Proxy.SelectedIndex=slots[i].Proxy.Items.Count-1;}}catch(Exception ex){Error(ex);}
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
                slot.Files=d.FileNames;slot.Launcher=launcher;slot.Client.Text=String.Join(" ; ",d.FileNames);Persist();
                Log("Client "+(index+1)+" launcher: "+slot.Launcher);
                Log("Yönlendirilecek client sayısı: "+patterns.Length);
            }catch(Exception ex){Error(ex);}
        }
    }
    IEnumerable<ProxyItem> AllProxies(){foreach(var slot in slots)foreach(ProxyItem item in slot.Proxy.Items)yield return item;}
    void SetBusy(bool busy) {
        load.Enabled=add.Enabled=connect.Enabled=second.Enabled=!busy;
        for(int i=0;i<2;i++){slots[i].Select.Enabled=slots[i].Proxy.Enabled=slots[i].Server.Enabled=slots[i].IP.Enabled=slots[i].Save.Enabled=!busy;slots[i].Launch.Enabled=busy&&ready&&(i==0||second.Checked);}
        update.Enabled=!busy&&!updateBusy;stop.Enabled=busy&&!starting;if(!busy)SetSecondState();
    }
    string Config() {
        var adapter=NetworkRoutes.Select();Log("Fiziksel ağ arayüzü: "+adapter.alias+"; geçit: "+adapter.gateway);
        var routes=new List<ClientRoute>();
        for(int i=0;i<(second.Checked?2:1);i++) {
            var s=slots[i];if(s.Proxy.SelectedItem==null)throw new Exception("Client "+(i+1)+" için proxy seçin.");
            if(s.Files.Length==0||!File.Exists(s.Launcher))throw new Exception("Client "+(i+1)+" için client ve launcher dosyalarını seçin.");
            routes.Add(new ClientRoute{Proxy=(ProxyItem)s.Proxy.SelectedItem,Patterns=ClientFiles.Patterns(s.Files,s.Launcher),IP=s.IP.Text,Tag="game-proxy-"+(i+1)});
        }
        if(second.Checked&&routes[0].Proxy.Host==routes[1].Proxy.Host&&routes[0].Proxy.Port==routes[1].Proxy.Port)throw new Exception("İkinci client için farklı bir proxy adresi seçin.");
        if(second.Checked&&String.Equals(Path.GetDirectoryName(slots[0].Launcher),Path.GetDirectoryName(slots[1].Launcher),StringComparison.OrdinalIgnoreCase))throw new Exception("İki client için iki ayrı oyun klasörü seçin. Aynı dosya yolundaki iki süreç ayrı proxyye atanamaz.");
        if(second.Checked&&RoutingConfig.SharedIP(routes.ToArray()))Log("Aynı sunucu IP'si: ayrı proxy için süreç eşleşmesi gerekli. Süreç bulunamayan sunucu bağlantıları engellenecek.");
        return json.Serialize(RoutingConfig.BuildMany(routes.ToArray(),adapter.alias));
    }
    void Connect(object sender,EventArgs e) {
        try {
            if(updateBusy)throw new Exception("Güncelleme işlemini bekleyin.");
            using(WindowsIdentity id=WindowsIdentity.GetCurrent())if(!new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator))throw new Exception("Uygulamayı yönetici olarak çalıştırın.");
            if(proxies.SelectedItem==null)throw new Exception("Proxy seçin.");
            if(selectedFiles.Length==0||!File.Exists(launcherPath))throw new Exception("Client ve launcher dosyalarını seçin.");
            string bin=Path.Combine(baseDir,"sing-box.exe"); 
            configFile=Path.Combine(Path.GetTempPath(),"metinproxy-"+Guid.NewGuid().ToString("N")+".json");
            File.WriteAllText(configFile,Config(),new UTF8Encoding(false));Persist();
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
                if(line.IndexOf("outbound/socks[game-proxy-",StringComparison.OrdinalIgnoreCase)>=0)status.Text="Proxy yönlendirme denemesi görüldü; oyun bağlantısı henüz doğrulanmadı.";
                if(line.Contains("sing-box started")){ready=true;for(int i=0;i<(second.Checked?2:1);i++)slots[i].Launch.Enabled=true;status.Text="Durum: Motor hazır — oyun bağlantısı henüz doğrulanmadı";}});
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
    void LaunchSlot(int index) {
        var slot=slots[index];
        try {
            if(!ready || engine==null || engine.HasExited)throw new Exception("Motor hazır değil.");
            Process.Start(new ProcessStartInfo {FileName=slot.Launcher,WorkingDirectory=Path.GetDirectoryName(slot.Launcher),UseShellExecute=true});
            Log("Client "+(index+1)+" açıldı. Oyun bağlantılarında game-proxy-"+(index+1)+" çıkışını doğrulayın.");
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
                var profiles=new List<ProxyItem>();foreach(ProxyItem p in AllProxies())profiles.Add(p);
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


