using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
partial class MainForm {
    Label latencyCaption=new Label(),latencyValue=new Label(),exitCaption=new Label(),exitValue=new Label(),countryCaption=new Label(),countryValue=new Label();PremiumButton measureProxy;
    bool proxyProbeBusy;int proxyProbeGeneration;DateTime nextProxyProbe=DateTime.MinValue,nextProxyLookup=DateTime.MinValue,lastManualProbe=DateTime.MinValue;
    void BuildProxyProbe(){
        latencyCaption.Text="TCP gecikmesi";exitCaption.Text="Çıkış IP";countryCaption.Text="Ülke";latencyValue.Text=exitValue.Text=countryValue.Text="—";
        Add(stateCard,latencyCaption,latencyValue,exitCaption,exitValue,countryCaption,countryValue);foreach(var label in new Label[]{latencyValue,exitValue,countryValue})label.TextAlign=ContentAlignment.MiddleRight;
        measureProxy=Button(stateCard,"Ölç","refresh","gold",delegate{PollProxyProbe(true);});fieldTips.SetToolTip(latencyCaption,"PC → seçili proxy TCP bağlantı süresi. Oyunun pingi değildir.");fieldTips.SetToolTip(exitCaption,"Seçili SOCKS5 proxy üzerinden HTTPS ile doğrulanan çıkış IP'si.");fieldTips.SetToolTip(countryCaption,"Çıkış IP'sinin coğrafi veritabanındaki ülkesi; fiziksel konum garantisi değildir.");
    }
    void ResetProxyProbe(){proxyProbeGeneration++;nextProxyProbe=nextProxyLookup=DateTime.MinValue;latencyValue.Text=exitValue.Text=countryValue.Text="—";foreach(var c in new Control[]{latencyValue,exitValue,countryValue,measureProxy})fieldTips.SetToolTip(c,"");}
    void PollProxyProbe(bool manual){
        if(previewMode||closing||proxyProbeBusy||initializing)return;var proxy=proxies.SelectedItem as ProxyItem;DateTime now=DateTime.UtcNow;measureProxy.Enabled=proxy!=null&&(now-lastManualProbe).TotalSeconds>=15;if(proxy==null)return;
        if(manual){if((now-lastManualProbe).TotalSeconds<15)return;lastManualProbe=now;}else if(now<nextProxyProbe||selectedPage!=0||WindowState==FormWindowState.Minimized||starting)return;
        bool lookup=manual||now>=nextProxyLookup;nextProxyProbe=now.AddMinutes(1);if(lookup)nextProxyLookup=now.AddMinutes(10);proxyProbeBusy=true;int generation=proxyProbeGeneration;measureProxy.Enabled=false;measureProxy.Text="…";
        if(lookup){exitValue.Text="Ölçülüyor…";countryValue.Text="—";}latencyValue.Text="Ölçülüyor…";var snapshot=new ProxyItem{Host=proxy.Host,Port=proxy.Port,User=proxy.User,Password=proxy.Password};
        Task.Run(delegate{var result=ProxyProbe.Measure(snapshot,lookup);UI(delegate{proxyProbeBusy=false;measureProxy.Text="Ölç";measureProxy.Enabled=proxies.SelectedItem!=null&&(DateTime.UtcNow-lastManualProbe).TotalSeconds>=15;if(generation!=proxyProbeGeneration)return;ApplyProxyProbe(result);});});
    }
    void ApplyProxyProbe(ProxyProbeResult result){
        latencyValue.Text=result.TcpMilliseconds.HasValue?result.TcpMilliseconds.Value+" ms":"Ulaşılamadı";fieldTips.SetToolTip(latencyValue,"TCP bağlantı süresi · "+result.CheckedAt.ToString("HH:mm:ss")+(result.Error==null?"":"\n"+result.Error));
        if(result.LookupAttempted){exitValue.Text=result.IP??"Doğrulanamadı";countryValue.Text=result.Country??"Alınamadı";string details=result.IP==null?result.Error:"Çıkış IP: "+result.IP+"\nÜlke: "+result.Country+"\nSağlayıcı: "+result.Provider+"\nSon kontrol: "+result.CheckedAt.ToString("HH:mm:ss");fieldTips.SetToolTip(exitValue,details);fieldTips.SetToolTip(countryValue,details);}
        fieldTips.SetToolTip(measureProxy,"TCP: 1 dk · IP/ülke: 10 dk. Son kontrol: "+result.CheckedAt.ToString("HH:mm:ss")+"\nÜlke ve sağlayıcı için ipwho.is kullanılır; proxy şifreniz bu servise gönderilmez.");
    }
}
