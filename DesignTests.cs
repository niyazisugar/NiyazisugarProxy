using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
partial class MainForm {
    void DesignAssert(bool condition,string message){if(!condition)throw new Exception("Design: "+message);}
    void CaptureDesign(string name){Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,ClientRectangle);bitmap.Save(name);}}
    public void VerifyDesign(){
        ClientSize=new Size(1484,1060);LayoutWindow();ShowPage(0);CaptureDesign("ui-connection.png");
        DesignAssert(slots.Length==1,"single client only");DesignAssert(!clientBadge.Text.Contains("Başarılı"),"unverified must not be success");
        ShowPage(1);CaptureDesign("ui-settings.png");ShowPage(2);CaptureDesign("ui-logs.png");
        AppendLogEntries("2026-10-15 05:30:01 INFO Uygulama başlatıldı.\n2026-10-15 05:30:02 WARN Süreç bilgisi okunamadı: Erişim reddedildi.");RefreshLogView(true);
        int original=visibleLogs.Count;var activeBefore=slots[0].Active;bool readyBefore=ready;SetLogPaused(true);AppendLogEntries("2026-10-15 05:30:03 ERROR Kontrol test kaydı.");RefreshLogView(false);
        DesignAssert(visibleLogs.Count==original&&slots[0].Active==activeBefore&&ready==readyBefore,"pause only freezes view");
        SetLogPaused(false);DesignAssert(visibleLogs.Count==original+1,"resume shows buffered log entries");search.Text="Erişim";RefreshLogView(true);DesignAssert(visibleLogs.Count==1,"search works");search.Clear();levelFilter.SelectedIndex=3;RefreshLogView(true);DesignAssert(visibleLogs.Count==1&&visibleLogs[0].Level=="HATA","level filter works");levelFilter.SelectedIndex=0;expandedLog=logBook.Snapshot()[original-1].Id;RefreshLogView(true);CaptureDesign("ui-log-controls.png");
        ShowPage(0);ClientSize=new Size(840,650);LayoutWindow();CaptureDesign("ui-compact-connection.png");ShowPage(1);CaptureDesign("ui-compact-settings.png");ShowPage(2);CaptureDesign("ui-compact-logs.png");
        ClientSize=new Size(640,520);for(int page=0;page<3;page++){ShowPage(page);CheckControls(pages[page]);}CaptureDesign("ui-minimum.png");
        uiDpi=1.5f;ClientSize=new Size(1920,1200);ShowPage(0);CheckControls(pages[0]);CaptureDesign("ui-dpi150-connection.png");ShowPage(1);CaptureDesign("ui-dpi150-settings.png");ShowPage(2);CaptureDesign("ui-dpi150-logs.png");
        uiDpi=1;theme.SelectedIndex=1;ClientSize=new Size(1484,1060);ShowPage(1);CaptureDesign("ui-light.png");theme.SelectedIndex=0;
        Console.WriteLine("Pause/resume, search, severity filtering, single-client UI, compact and 150% DPI checks passed.");
    }
    void CheckControls(Control root){foreach(Control child in root.Controls){if(child is Button||child is TextBox||child is ComboBox){DesignAssert(child.Width>0&&child.Height>0,"positive control size");DesignAssert(child.Left>=0&&child.Top>=0,"control origin");DesignAssert(child.Right<=root.ClientSize.Width+2,"control within parent: "+child.Text+" "+child.Right+"/"+root.ClientSize.Width);}CheckControls(child);}}
}
