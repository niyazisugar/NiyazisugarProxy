using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
partial class MainForm {
    void DesignAssert(bool condition,string message){if(!condition)throw new Exception("Design: "+message);}
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool MoveWindow(IntPtr handle,int x,int y,int width,int height,bool repaint);
    void SizePreview(int width,int height){ClientSize=new Size(width,height);MoveWindow(Handle,0,0,width,height,true);Application.DoEvents();LayoutWindow();DesignAssert(Width==width&&Height==height,"native capture size "+Width+"x"+Height+" expected "+width+"x"+height);}
    void CaptureDesign(string name){Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,ClientRectangle);bitmap.Save(name);}}
    public void VerifyDesign(){
        MaximumSize=new Size(8192,8192);SizePreview(740,530);LayoutWindow();ShowPage(0);CheckCompactViewport();CaptureDesign("ui-connection.png");
        DesignAssert(slots.Length==1,"single client only");DesignAssert(!clientBadge.Text.Contains("Başarılı"),"unverified must not be success");ready=true;RefreshConnectionStatus();DesignAssert(clientBadge.Text=="Doğrulanmadı","engine ready alone is not game verification");ready=false;RefreshConnectionStatus();
        ShowPage(1);CaptureDesign("ui-settings.png");ShowPage(2);CaptureDesign("ui-logs.png");
        AppendLogEntries("2026-10-15 05:30:01 INFO Uygulama başlatıldı.\n2026-10-15 05:30:02 WARN Süreç bilgisi okunamadı: Erişim reddedildi.");RefreshLogView(true);
        int original=visibleLogs.Count;var activeBefore=slots[0].Active;bool readyBefore=ready;SetLogPaused(true);AppendLogEntries("2026-10-15 05:30:03 ERROR Kontrol test kaydı.");RefreshLogView(false);
        DesignAssert(visibleLogs.Count==original&&slots[0].Active==activeBefore&&ready==readyBefore,"pause only freezes view");
        SetLogPaused(false);DesignAssert(visibleLogs.Count==original+1,"resume shows buffered log entries");search.Text="Erişim";RefreshLogView(true);DesignAssert(visibleLogs.Count==1,"search works");search.Clear();levelFilter.SelectedIndex=3;RefreshLogView(true);DesignAssert(visibleLogs.Count==1&&visibleLogs[0].Level=="HATA","level filter works");levelFilter.SelectedIndex=0;expandedLog=logBook.Snapshot()[original-1].Id;RefreshLogView(true);CaptureDesign("ui-log-controls.png");
        ShowPage(0);SizePreview(840,650);LayoutWindow();CaptureDesign("ui-compact-connection.png");ShowPage(1);CaptureDesign("ui-compact-settings.png");ShowPage(2);CaptureDesign("ui-compact-logs.png");
        SizePreview(640,460);for(int page=0;page<3;page++){ShowPage(page);CheckControls(pages[page]);}CaptureDesign("ui-minimum.png");
        uiDpi=1.5f;SizePreview(740,530);ShowPage(0);CheckCompactViewport();CaptureDesign("ui-dpi150-small.png");SizePreview(1920,1200);ShowPage(0);CheckControls(pages[0]);CaptureDesign("ui-dpi150-connection.png");ShowPage(1);CaptureDesign("ui-dpi150-settings.png");ShowPage(2);CaptureDesign("ui-dpi150-logs.png");
        uiDpi=1;theme.SelectedIndex=1;SizePreview(1484,1060);ShowPage(1);CaptureDesign("ui-light.png");theme.SelectedIndex=0;
        Console.WriteLine("Pause/resume, search, severity filtering, single-client UI, compact and 150% DPI checks passed.");
    }
    void CheckCompactViewport(){DesignAssert(header.Height<65,"single-row compact header");DesignAssert(stateCard.Left>=connectionCard.Right,"two-column connection layout");DesignAssert(nav[0].Top==nav[3].Top&&nav[3].Right<minimize.Left,"navigation fits one row");for(int page=0;page<3;page++){ShowPage(page);CheckControls(pages[page]);DesignAssert(!pages[page].VerticalScroll.Visible&&!pages[page].HorizontalScroll.Visible,"page fits compact viewport "+page);}ShowPage(0);DesignAssert(connect.Bottom<=connectionCard.Height&&noticeCard.Bottom<=tabs.Height&&footer.Bottom<=tabs.Height,"actions and footer visible");}
    void CheckControls(Control root){foreach(Control child in root.Controls){if(child is Button||child is TextBox||child is ComboBox){DesignAssert(child.Width>0&&child.Height>0,"positive control size");DesignAssert(child.Left>=0&&child.Top>=0,"control origin");DesignAssert(child.Right<=root.ClientSize.Width+2,"control within parent: "+child.Text+" "+child.Right+"/"+root.ClientSize.Width);}CheckControls(child);}}
}
