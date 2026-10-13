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
        public Button Select, Launch, Save, Connect, Stop, Add;public ClientRoute Active;
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
    Button load,add,connect,stop,update,sendReport,minimize,close;
    CheckBox second=new CheckBox(); ComboBox theme=new ComboBox(),accent=new ComboBox();
    Panel tabs=new Panel(); Panel header;Panel[] pages;Button[] navigation=new Button[3];int selectedPage; AppPreferences preferences;
    UpdateRelease availableUpdate; bool updateBusy; string localLogPath;
    SlotRelay[] relays;LiveRouting liveRoutes;
    Process engine; string configFile; bool starting,closing,ready,initializing=true,previewMode,proxyLoading;
    readonly JavaScriptSerializer json=new JavaScriptSerializer();
    readonly string baseDir=AppDomain.CurrentDomain.BaseDirectory;
    public MainForm() : this(false) {}
    public MainForm(bool preview) {
        previewMode=preview;preferences=AppPreferences.Load();
        Text="NiyazisugarProxy"; FormBorderStyle=FormBorderStyle.None;MinimumSize=new Size(640,520);ClientSize=WindowGeometry.Restore(preferences.WindowWidth,preferences.WindowHeight,Screen.FromPoint(Cursor.Position).WorkingArea.Size);
         StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10); DoubleBuffered=true;
        header=new Panel{Dock=DockStyle.Top,Height=88};Controls.Add(header);
        header.Controls.Add(new Label{Text="NIYAZISUGAR",Font=new Font("Segoe UI",21,FontStyle.Bold),AutoSize=true,Location=new Point(26,14)});
        header.Controls.Add(new Label{Text="PROXY  /  v"+Updates.Current.ToString(3),AutoSize=true,Location=new Point(29,53)});
        header.MouseDown+=DragWindow;foreach(Control c in header.Controls)c.MouseDown+=DragWindow;
        minimize=MakeButton("—",header,890,20,40,delegate{WindowState=FormWindowState.Minimized;});
        close=MakeButton("×",header,936,20,40,delegate{Close();});minimize.TabStop=false;close.TabStop=false;
        tabs.SetBounds(22,145,956,613);tabs.Font=Font;Controls.Add(tabs);
        var dashboard=new Panel{Dock=DockStyle.Fill,AutoScroll=true};var settings=new Panel{Dock=DockStyle.Fill,AutoScroll=true};var activity=new Panel{Dock=DockStyle.Fill};
        pages=new Panel[]{dashboard,settings,activity};tabs.Controls.AddRange(pages);
        for(int nav=0;nav<3;nav++){int page=nav;navigation[nav]=MakeButton(new string[]{"Clientler","Ayarlar","Loglar"}[nav],this,22+nav*155,99,145,delegate{ShowPage(page);});}
        load=MakeButton("Liste yükle",dashboard,18,18,130,LoadList);
        add=MakeButton("Proxy ekle",dashboard,158,18,130,AddProxy);
        second.Text="2. client etkin";second.SetBounds(730,23,185,30);second.Checked=preferences.SecondEnabled;dashboard.Controls.Add(second);
        second.CheckedChanged+=delegate{if(!second.Checked&&slots[1].Active!=null){second.Checked=true;return;}SetSecondState();LayoutWindow();Persist();};
        for(int i=0;i<2;i++)BuildSlot(dashboard,i);
        SetSecondState();
        connect=MakeButton("1. clienti bağla",dashboard,18,496,210,Connect);
        stop=MakeButton("Tümünü durdur",dashboard,240,496,170,Stop);stop.Enabled=false;
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
        if(!preview)RestoreProxies();foreach(var slot in slots)slot.Proxy.SelectedIndexChanged+=delegate{SaveProxies();};
        initializing=false;PrepareFields(this);ShowPage(0);ApplyTheme();Resize+=delegate{if(!initializing&&WindowState!=FormWindowState.Minimized){LayoutWindow();RoundWindow();}};ResizeEnd+=delegate{Persist();};LayoutWindow();RoundWindow();SetBusy(false);
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
        s.Connect=MakeButton("Bağlan",s.Card,18,228,128,delegate{ActivateSlot(index);});
        s.Stop=MakeButton("Durdur",s.Card,156,228,116,delegate{StopSlot(index);});s.Stop.Enabled=false;
        s.Add=MakeButton("Proxy ekle",s.Card,282,228,150,delegate{AddSlotProxy(index);});
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
    protected override void WndProc(ref Message message) {
        if(message.Msg==0x84&&WindowState==FormWindowState.Normal){base.WndProc(ref message);if(message.Result==new IntPtr(1)){long bits=message.LParam.ToInt64();Point point=PointToClient(new Point((short)(bits&0xffff),(short)((bits>>16)&0xffff)));int hit=WindowGeometry.HitTest(point,ClientSize,10);if(hit!=1)message.Result=new IntPtr(hit);}return;}
        base.WndProc(ref message);
    }
    void LayoutWindow(){
        if(pages==null||pages.Length!=3||connect==null)return;
        SuspendLayout();try{
            minimize.Left=ClientSize.Width-110;close.Left=ClientSize.Width-64;
            tabs.SetBounds(22,145,Math.Max(1,ClientSize.Width-44),Math.Max(1,ClientSize.Height-167));
            var dashboard=pages[0];dashboard.AutoScrollPosition=Point.Empty;
            int width=Math.Max(520,tabs.ClientSize.Width-56);bool compact=width<820;int cardHeight=compact?296:245;
            second.Left=Math.Max(306,width-175);
            for(int i=0;i<2;i++){
                var slot=slots[i];slot.Card.Visible=i==0||second.Checked;
                slot.Card.SetBounds(18,74+i*(cardHeight+16),width,cardHeight);
                slot.Launch.SetBounds(width-158,10,140,36);int controlsTop=compact?250:190;slot.Connect.SetBounds(18,controlsTop,128,36);slot.Stop.SetBounds(156,controlsTop,116,36);slot.Add.SetBounds(282,controlsTop,150,36);
                if(compact){slot.Proxy.SetBounds(88,52,width-106,30);slot.Server.SetBounds(88,92,width-106,30);slot.Client.SetBounds(18,138,width-176,28);slot.Select.SetBounds(width-158,134,140,36);slot.IP.SetBounds(110,188,width-312,28);slot.Save.SetBounds(width-184,182,166,36);}
                else{slot.Proxy.SetBounds(88,52,(width-260)/2,30);slot.Server.SetBounds((width/2)+58,52,(width/2)-76,30);slot.Client.SetBounds(18,98,width-176,28);slot.Select.SetBounds(width-158,94,140,36);slot.IP.SetBounds(110,143,250,28);slot.Save.SetBounds(374,138,170,36);}
                foreach(Control label in slot.Card.Controls)if(label is Label){if(label.Text=="Sunucu")label.Location=compact?new Point(18,96):new Point(width/2-16,56);if(label.Text=="Sunucu IP")label.Top=compact?194:149;if(label.Text=="EXE + BIN seçimi desteklenir"){label.Location=compact?new Point(18,225):new Point(567,149);label.Visible=compact||width>=840;}}
            }
            int actions=74+(second.Checked?2:1)*(cardHeight+16);
            connect.SetBounds(18,actions,210,36);stop.SetBounds(240,actions,120,36);status.SetBounds(18,actions+54,width,44);
            dashboard.AutoScrollMinSize=new Size(0,actions+116);
            var settings=pages[1];settings.AutoScrollPosition=Point.Empty;
            foreach(Control card in settings.Controls)if(card is RoundedPanel){card.Width=width;foreach(Control label in card.Controls)if(label is Label&&label.Top>=120){label.AutoSize=false;label.Size=new Size(width-40,48);}}
            settings.AutoScrollMinSize=new Size(0,472);
            logs.SetBounds(18,18,tabs.ClientSize.Width-36,Math.Max(100,tabs.ClientSize.Height-98));sendReport.SetBounds(18,tabs.ClientSize.Height-60,220,36);
            foreach(Control label in pages[2].Controls)if(label is Label){label.AutoSize=false;label.SetBounds(254,tabs.ClientSize.Height-58,Math.Max(100,tabs.ClientSize.Width-274),46);}
        }finally{ResumeLayout(true);}
    }
    void RoundWindow(){using(var shape=RoundedPanel.Shape(ClientRectangle,20)){var old=Region;Region=new Region(shape);if(old!=null)old.Dispose();}}
    void ApplyTheme(){if(theme.SelectedIndex<0)return;bool light=theme.SelectedIndex==1;Color background=light?Color.FromArgb(239,241,247):Color.FromArgb(15,18,27);Color card=light?Color.White:Color.FromArgb(25,30,43);Color field=light?Color.FromArgb(231,235,244):Color.FromArgb(35,42,59);Color foreground=light?Color.FromArgb(30,36,50):Color.FromArgb(229,235,247);Color tint=accent.SelectedIndex==1?Color.FromArgb(50,120,223):accent.SelectedIndex==2?Color.FromArgb(30,148,111):Color.FromArgb(115,81,217);ThemeControl(this,background,card,field,foreground,tint);for(int i=0;i<navigation.Length;i++){navigation[i].BackColor=i==selectedPage?tint:field;navigation[i].ForeColor=i==selectedPage?Color.White:foreground;}Invalidate(true);}
    void ThemeControl(Control root,Color background,Color card,Color field,Color foreground,Color tint){root.ForeColor=root is Button?Color.White:foreground;root.BackColor=root is RoundedPanel?card:root is Button?tint:root is TextBox||root is ComboBox?field:background;var panel=root as RoundedPanel;if(panel!=null){panel.Surface=card;panel.Border=field;}foreach(Control child in root.Controls)ThemeControl(child,root is RoundedPanel?card:background,card,field,foreground,tint);}
    void ShowPage(int index){selectedPage=index;for(int i=0;i<pages.Length;i++)pages[i].Visible=i==index;ApplyTheme();}
    void SetSecondState(){if(connect==null||slots[1].Connect==null)return;SetBusy(starting||engine!=null);}
    void PrepareFields(Control root){foreach(Control child in root.Controls){var input=child as TextBox;if(input!=null)input.BorderStyle=BorderStyle.None;var combo=child as ComboBox;if(combo!=null){combo.FlatStyle=FlatStyle.Flat;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=23;combo.DrawItem+=delegate(object sender,DrawItemEventArgs e){var box=(ComboBox)sender;using(var brush=new SolidBrush(box.BackColor))e.Graphics.FillRectangle(brush,e.Bounds);string text=e.Index>=0?box.Items[e.Index].ToString():box.Text;TextRenderer.DrawText(e.Graphics,text,box.Font,e.Bounds,box.ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);if((e.State&DrawItemState.Selected)!=0)e.DrawFocusRectangle();};}PrepareFields(child);}}
    void RefreshServers(){for(int i=0;i<2;i++){string ip=slots[i].IP.Text;slots[i].Server.Items.Clear();slots[i].Server.Items.Add("Kayıtlı sunucu seç");foreach(var profile in preferences.Servers)slots[i].Server.Items.Add(profile);slots[i].Server.SelectedIndex=0;slots[i].IP.Text=ip;}}
    void SaveServer(int index){try{var s=slots[index];string cidr=RoutingConfig.ValidateFallback(s.IP.Text);if(cidr==null)throw new Exception("Önce sunucu IP'sini girin.");using(var dialog=new Form{Text="Sunucuyu kaydet",ClientSize=new Size(420,130),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){AddLabel(dialog,"Sunucu adı (ör. Sezar2)",15,12,10,false);var name=new TextBox{Left=15,Top=42,Width=390,MaxLength=80};dialog.Controls.Add(name);var selected=s.Server.SelectedItem as ServerProfile;if(selected!=null)name.Text=selected.Name;var ok=MakeButton("Kaydet",dialog,275,84,130,delegate{});ok.DialogResult=DialogResult.OK;dialog.AcceptButton=ok;if(dialog.ShowDialog(this)!=DialogResult.OK)return;if(String.IsNullOrWhiteSpace(name.Text))throw new Exception("Sunucu adı yazın.");preferences.PutServer(name.Text.Trim(),cidr.Substring(0,cidr.Length-3));RefreshServers();s.Server.SelectedItem=preferences.Servers.Find(delegate(ServerProfile item){return item.Name.Equals(name.Text.Trim(),StringComparison.OrdinalIgnoreCase);});Persist();Log("Sunucu kaydedildi: "+name.Text.Trim());}}catch(Exception ex){Error(ex);}}
    void Persist(){if(initializing||previewMode)return;try{preferences.Light=theme.SelectedIndex==1;preferences.Accent=accent.SelectedIndex;preferences.SecondEnabled=second.Checked;if(WindowState==FormWindowState.Normal){preferences.WindowWidth=ClientSize.Width;preferences.WindowHeight=ClientSize.Height;}for(int i=0;i<2;i++)preferences.Slots[i]=new SlotPreference{Launcher=slots[i].Launcher,Files=slots[i].Files,IP=slots[i].IP.Text};preferences.Save();}catch(Exception ex){Log("Ayarlar kaydedilemedi: "+ex.Message);}}
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
    void RestoreProxies(){try{var saved=ProxyVault.Load();proxyLoading=true;for(int i=0;i<2;i++){slots[i].Proxy.Items.AddRange(saved.Items);int selected=saved.Selected[i];slots[i].Proxy.SelectedIndex=saved.Items.Length==0?-1:selected>=0&&selected<saved.Items.Length?selected:Math.Min(i,saved.Items.Length-1);}}catch(Exception ex){logs.AppendText("Kayıtlı proxy listesi okunamadı; kayıt dosyası korundu. "+ex.GetType().Name+Environment.NewLine);}finally{proxyLoading=false;}}
    void SaveProxies(){if(initializing||previewMode||proxyLoading)return;try{var items=new List<ProxyItem>();foreach(ProxyItem item in proxies.Items)items.Add(item);ProxyVault.Save(new ProxySnapshot{Items=items.ToArray(),Selected=new int[]{slots[0].Proxy.SelectedIndex,slots[1].Proxy.SelectedIndex}});}catch(Exception ex){Log("Proxy listesi kaydedilemedi: "+ex.GetType().Name);}}
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
        using(Form d=new Form {Text="Proxy ekle",ClientSize=new Size(480,125),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
            var label=new Label {Text="IPv4:PORT veya IPv4:PORT:KULLANICI:ŞİFRE",AutoSize=true,Location=new Point(12,12)};
            var input=new TextBox {Location=new Point(12,40),Width=450,UseSystemPasswordChar=true};
            var ok=new Button {Text="Ekle",Location=new Point(362,78),DialogResult=DialogResult.OK};
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
                slot.Files=d.FileNames;slot.Launcher=launcher;slot.Client.Text=String.Join(" ; ",d.FileNames);Persist();
                Log("Client "+(index+1)+" launcher: "+slot.Launcher);
                Log("Yönlendirilecek client sayısı: "+patterns.Length);
            }catch(Exception ex){Error(ex);}
        }
    }
    IEnumerable<ProxyItem> AllProxies(){foreach(var slot in slots)foreach(ProxyItem item in slot.Proxy.Items)yield return item;}
    void SetBusy(bool busy) {
        load.Enabled=!starting&&slots[0].Active==null&&slots[1].Active==null;add.Enabled=!starting;second.Enabled=!starting;
        connect.Enabled=!starting&&!updateBusy&&slots[0].Active==null;
        for(int i=0;i<2;i++){var slot=slots[i];bool editable=!starting&&slot.Active==null&&(i==0||second.Checked);slot.Select.Enabled=slot.Proxy.Enabled=slot.Server.Enabled=slot.IP.Enabled=slot.Save.Enabled=slot.Add.Enabled=editable;slot.Connect.Enabled=editable&&!updateBusy;slot.Stop.Enabled=!starting&&slot.Active!=null;slot.Launch.Enabled=ready&&slot.Active!=null;}
        update.Enabled=!busy&&!updateBusy;stop.Enabled=!starting&&engine!=null;
    }
    ClientRoute ReadSlot(int index){var slot=slots[index];var proxy=slot.Proxy.SelectedItem as ProxyItem;if(proxy==null)throw new Exception("Client "+(index+1)+" için proxy seçin.");if(slot.Files.Length==0||!File.Exists(slot.Launcher))throw new Exception("Client "+(index+1)+" için client ve launcher dosyalarını seçin.");RoutingConfig.ValidateFallback(slot.IP.Text);return new ClientRoute{Proxy=proxy,Patterns=ClientFiles.Patterns(slot.Files,slot.Launcher),IP=slot.IP.Text,Tag="game-proxy-"+(index+1)};}
    void AddSlotProxy(int index){int count=proxies.Items.Count;int first=slots[0].Proxy.SelectedIndex,other=slots[1].Proxy.SelectedIndex;AddProxy(null,EventArgs.Empty);if(proxies.Items.Count>count){slots[index].Proxy.SelectedIndex=proxies.Items.Count-1;if(index==1)slots[0].Proxy.SelectedIndex=first;else slots[1].Proxy.SelectedIndex=other;}SetBusy(starting||engine!=null);}
    void Connect(object sender,EventArgs e){ActivateSlot(0);}
    void ActivateSlot(int index){
        if(starting||slots[index].Active!=null)return;
        try{
            if(updateBusy)throw new Exception("Güncelleme işlemini bekleyin.");
            using(WindowsIdentity id=WindowsIdentity.GetCurrent())if(!new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator))throw new Exception("Uygulamayı yönetici olarak çalıştırın.");
            var candidate=ReadSlot(index);var other=slots[1-index].Active;
            if(other!=null){if(other.Proxy.Host==candidate.Proxy.Host&&other.Proxy.Port==candidate.Proxy.Port)throw new Exception("Farklı proxy adresleri seçin.");foreach(string pattern in candidate.Patterns)if(Array.IndexOf(other.Patterns,pattern)>=0)throw new Exception("Clientler için farklı oyun klasörleri seçin.");
                if(RoutingConfig.ValidateFallback(other.IP)!=null&&RoutingConfig.ValidateFallback(other.IP)==RoutingConfig.ValidateFallback(candidate.IP)){
                    if(MessageBox.Show(this,"Aynı sunucuya iki farklı proxy için süreç eşleşmesi gerekiyor. Koruma süreci gizlerse yeni bağlantılar engellenebilir; ilk oyun tekrar bağlanamayabilir. Devam edilsin mi?","İki client",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return;
                }
            }
            bool firstEngine=engine==null;
            if(firstEngine){liveRoutes=new LiveRouting();relays=new SlotRelay[]{new SlotRelay(),new SlotRelay()};}
            relays[index].SetProxy(candidate.Proxy);slots[index].Active=candidate;
            liveRoutes.Update(new ClientRoute[]{slots[0].Active,slots[1].Active});Persist();
            if(firstEngine){var adapter=NetworkRoutes.Select();configFile=Path.Combine(Path.GetTempPath(),"metinproxy-"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(configFile,json.Serialize(liveRoutes.Config(new int[]{relays[0].Port,relays[1].Port},adapter.alias)),new UTF8Encoding(false));starting=true;ready=false;SetBusy(true);status.Text="Motor başlatılıyor…";var worker=new Thread(delegate(){StartEngine(Path.Combine(baseDir,"sing-box.exe"));});worker.IsBackground=true;worker.Start();}
            else{SetBusy(true);Log("Client "+(index+1)+" kuralları güncellendi; motor yeniden başlatılmadı. Yeni kuralların uygulanması birkaç saniye sürebilir.");}
        }catch(Exception ex){if(relays!=null)relays[index].Stop();slots[index].Active=null;if(engine==null){DisposeLive();CleanConfig();}else try{liveRoutes.Update(new ClientRoute[]{slots[0].Active,slots[1].Active});}catch(Exception updateError){Log("Kurallar geri yüklenemedi: "+updateError.Message);}SetBusy(starting||engine!=null);Error(ex);}
    }
    void StopSlot(int index){if(starting||slots[index].Active==null)return;try{relays[index].Stop();slots[index].Active=null;liveRoutes.Update(new ClientRoute[]{slots[0].Active,slots[1].Active});Log("Client "+(index+1)+" proxy bağlantıları durduruldu; diğer client değişmedi.");SetBusy(engine!=null);}catch(Exception ex){Error(ex);}}
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
                string line=e.Data; UI(delegate {line=Regex.Replace(line,"\\x1B\\[[0-?]*[ -/]*[@-~]","");Log(line);
                if(line.IndexOf("failed to search process",StringComparison.OrdinalIgnoreCase)>=0)status.Text="UYARI: Süreç okunamadı; oyun proxy kullanımı doğrulanmadı.";
                if(line.IndexOf("outbound/socks[game-proxy-",StringComparison.OrdinalIgnoreCase)>=0)status.Text="Proxy yönlendirme denemesi görüldü; oyun bağlantısı henüz doğrulanmadı.";
                if(line.Contains("sing-box started")){ready=true;SetBusy(true);status.Text="Durum: Motor hazır — oyun bağlantısı henüz doğrulanmadı";}});
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
    void Finished() {starting=false;ready=false;DisposeLive();CleanConfig();SetBusy(false);status.Text="Durum: Bağlı değil";}
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
        DisposeLive();CleanConfig();
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
        updateBusy=false;SetBusy(starting||engine!=null);
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

