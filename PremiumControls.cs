using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
static class Palette {
    public static Color Background=Color.FromArgb(13,20,26),Card=Color.FromArgb(23,32,40),Field=Color.FromArgb(16,24,31),Border=Color.FromArgb(49,64,77),Text=Color.FromArgb(234,238,249),Muted=Color.FromArgb(157,173,193),Gold=Color.FromArgb(232,190,104),Turquoise=Color.FromArgb(20,211,204),Red=Color.FromArgb(255,77,82);
    public static Color Tint(Color a,Color b,float value){return Color.FromArgb((int)(a.R+(b.R-a.R)*value),(int)(a.G+(b.G-a.G)*value),(int)(a.B+(b.B-a.B)*value));}
}
static class Symbols {
    public static void Draw(Graphics g,string name,RectangleF rect,Color color){if(rect.Width<=0||rect.Height<=0)return;var state=g.Save();g.TranslateTransform(rect.X,rect.Y);g.ScaleTransform(rect.Width/24f,rect.Height/24f);g.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(color,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;using(var brush=new SolidBrush(color)){
        switch(name){
        case "logo":using(var gold=new LinearGradientBrush(new Rectangle(0,0,24,24),Color.FromArgb(255,224,153),Color.FromArgb(164,117,45),60f))using(var path=new GraphicsPath()){path.FillMode=FillMode.Alternate;path.AddPolygon(new PointF[]{new PointF(9.763f,8.492f),new PointF(7.322f,6.646f),new PointF(6.508f,6.646f),new PointF(5.695f,5.538f),new PointF(3.661f,4.800f),new PointF(2.847f,3.692f),new PointF(0.814f,2.954f),new PointF(0.814f,2.215f),new PointF(2.441f,1.846f),new PointF(2.847f,1.108f),new PointF(5.288f,0.000f),new PointF(5.695f,0.738f),new PointF(7.729f,1.477f),new PointF(8.542f,2.585f),new PointF(10.576f,3.323f),new PointF(11.390f,4.431f),new PointF(13.424f,5.169f),new PointF(14.237f,6.277f),new PointF(15.051f,6.277f),new PointF(16.271f,7.385f),new PointF(16.271f,12.554f),new PointF(13.831f,11.446f),new PointF(13.017f,10.338f),new PointF(12.203f,10.338f)});path.AddPolygon(new PointF[]{new PointF(4.475f,22.154f),new PointF(4.475f,23.631f),new PointF(4.068f,23.631f),new PointF(0.000f,21.046f),new PointF(0.000f,5.538f),new PointF(4.475f,8.492f)});path.AddPolygon(new PointF[]{new PointF(16.678f,17.354f),new PointF(22.780f,21.046f),new PointF(22.373f,22.154f),new PointF(19.525f,23.262f),new PointF(19.119f,24.000f),new PointF(16.678f,22.892f),new PointF(15.864f,21.785f),new PointF(13.831f,21.046f),new PointF(13.017f,19.938f),new PointF(12.203f,19.938f),new PointF(10.576f,18.462f),new PointF(9.763f,18.462f),new PointF(9.356f,17.723f),new PointF(8.542f,17.723f),new PointF(7.729f,16.985f),new PointF(7.729f,11.446f)});path.AddPolygon(new PointF[]{new PointF(19.525f,15.508f),new PointF(19.525f,0.369f),new PointF(19.932f,0.369f),new PointF(24.000f,2.954f),new PointF(24.000f,18.462f),new PointF(22.780f,18.092f),new PointF(21.966f,16.985f),new PointF(21.153f,16.985f)});g.FillPath(gold,path);}break;
        case "link":g.TranslateTransform(12,12);g.RotateTransform(43);using(var first=RoundedPanel.Shape(new Rectangle(-4,-11,8,14),4))using(var second=RoundedPanel.Shape(new Rectangle(-4,-3,8,14),4)){g.DrawPath(pen,first);g.DrawPath(pen,second);}g.DrawLine(pen,0,-5,0,5);break;
        case "folder":g.DrawLines(pen,new Point[]{new Point(2,6),new Point(2,21),new Point(22,21),new Point(22,8),new Point(11,8),new Point(8,4),new Point(2,4),new Point(2,6)});break;
        case "list":for(int y=5;y<23;y+=7){g.FillRectangle(brush,1,y,2,2);g.DrawLine(pen,7,y+1,22,y+1);}break;
        case "gear":var points=new PointF[32];for(int i=0;i<32;i++){double a=i*Math.PI/16;float r=(i%4==1||i%4==2)?11:8.5f;points[i]=new PointF(12+(float)Math.Cos(a)*r,12+(float)Math.Sin(a)*r);}using(var path=new GraphicsPath()){path.FillMode=FillMode.Alternate;path.AddPolygon(points);path.AddEllipse(8,8,8,8);g.FillPath(brush,path);}break;
        case "file":g.DrawLines(pen,new Point[]{new Point(5,2),new Point(15,2),new Point(20,7),new Point(20,22),new Point(5,22),new Point(5,2)});g.DrawLines(pen,new Point[]{new Point(14,2),new Point(14,8),new Point(20,8)});g.DrawLine(pen,9,12,16,12);g.DrawLine(pen,9,16,16,16);break;
        case "save":g.DrawRectangle(pen,3,2,18,20);g.DrawRectangle(pen,7,3,9,6);g.DrawRectangle(pen,7,14,10,8);break;
        case "swap":g.DrawLine(pen,3,7,21,7);g.DrawLines(pen,new Point[]{new Point(16,2),new Point(21,7),new Point(16,12)});g.DrawLine(pen,21,17,3,17);g.DrawLines(pen,new Point[]{new Point(8,12),new Point(3,17),new Point(8,22)});break;
        case "play":g.FillPolygon(brush,new Point[]{new Point(7,3),new Point(21,12),new Point(7,21)});break;
        case "stop":using(var shape=RoundedPanel.Shape(new Rectangle(4,4,16,16),3))g.FillPath(brush,shape);break;
        case "pause":g.FillRectangle(brush,5,3,5,18);g.FillRectangle(brush,14,3,5,18);break;
        case "plus":g.DrawLine(pen,12,2,12,22);g.DrawLine(pen,2,12,22,12);break;
        case "upload":g.DrawLines(pen,new Point[]{new Point(2,14),new Point(2,22),new Point(22,22),new Point(22,14)});g.DrawLine(pen,12,17,12,2);g.DrawLines(pen,new Point[]{new Point(6,8),new Point(12,2),new Point(18,8)});break;
        case "search":g.DrawEllipse(pen,2,2,14,14);g.DrawLine(pen,14,14,22,22);break;
        case "copy":g.DrawRectangle(pen,8,6,13,16);g.DrawLines(pen,new Point[]{new Point(16,3),new Point(3,3),new Point(3,18)});break;
        case "refresh":g.DrawArc(pen,3,3,18,18,30,285);g.DrawLines(pen,new Point[]{new Point(17,1),new Point(21,6),new Point(15,7)});break;
        case "shield":case "shield-check":g.DrawLines(pen,new Point[]{new Point(12,2),new Point(22,6),new Point(21,15),new Point(18,20),new Point(12,24),new Point(6,20),new Point(3,15),new Point(2,6),new Point(12,2)});if(name=="shield-check")g.DrawLines(pen,new Point[]{new Point(7,12),new Point(11,16),new Point(17,9)});else{using(var font=new Font("Segoe UI",14,FontStyle.Bold,GraphicsUnit.Pixel))g.DrawString("?",font,brush,6,4);}break;
        case "check-circle":g.FillEllipse(brush,0,0,24,24);using(var dark=new Pen(Color.FromArgb(7,38,34),2)){g.DrawLines(dark,new Point[]{new Point(6,12),new Point(10,16),new Point(18,7)});}break;
        case "caret":g.DrawLines(pen,new Point[]{new Point(8,5),new Point(15,12),new Point(8,19)});break;
        case "check":g.DrawLines(pen,new Point[]{new Point(4,12),new Point(10,18),new Point(21,5)});break;
        case "info":case "help":g.DrawEllipse(pen,2,2,20,20);using(var font=new Font("Segoe UI",17,FontStyle.Bold,GraphicsUnit.Pixel))g.DrawString(name=="help"?"?":"i",font,brush,7,1);break;
        case "database":g.DrawEllipse(pen,3,2,18,7);g.DrawLine(pen,3,6,3,20);g.DrawLine(pen,21,6,21,20);g.DrawArc(pen,3,16,18,7,0,180);g.DrawArc(pen,3,9,18,7,0,180);break;
        case "cube":g.DrawPolygon(pen,new Point[]{new Point(12,1),new Point(22,7),new Point(22,18),new Point(12,24),new Point(2,18),new Point(2,7)});g.DrawLines(pen,new Point[]{new Point(2,7),new Point(12,13),new Point(22,7)});g.DrawLine(pen,12,13,12,24);break;
        case "palette":g.FillEllipse(brush,1,2,22,19);using(var dark=new SolidBrush(Palette.Card)){g.FillEllipse(dark,14,14,10,10);g.FillEllipse(dark,5,8,3,3);g.FillEllipse(dark,10,5,3,3);g.FillEllipse(dark,16,7,3,3);}break;
        case "moon":g.FillEllipse(brush,3,2,18,20);using(var dark=new SolidBrush(Palette.Field))g.FillEllipse(dark,10,-1,16,17);break;
        case "chevron":g.DrawLines(pen,new Point[]{new Point(5,9),new Point(12,16),new Point(19,9)});break;
        case "arrow":g.DrawLine(pen,2,12,22,12);g.DrawLines(pen,new Point[]{new Point(16,6),new Point(22,12),new Point(16,18)});break;
        }
    }}g.Restore(state);}
}
class PremiumCard : Panel {
    public Color BorderColor=Color.Empty,FillColor=Color.Empty;public int Radius=12;public bool Gradient=true;
    public PremiumCard(){DoubleBuffered=true;BackColor=Palette.Card;}
    protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?Palette.Background:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=RoundedPanel.Shape(ClientRectangle,Radius))using(var brush=new LinearGradientBrush(ClientRectangle,FillColor.IsEmpty?Palette.Card:FillColor,FillColor.IsEmpty?Palette.Tint(Palette.Card,Palette.Background,.25f):FillColor,65f))using(var pen=new Pen(BorderColor.IsEmpty?Palette.Border:BorderColor)){e.Graphics.FillPath(brush,path);e.Graphics.DrawPath(pen,path);}}
}
class PremiumIcon : Control {
    public string Symbol="info";public Color Ink=Color.Empty;
    public PremiumIcon(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;}
    protected override void OnPaint(PaintEventArgs e){Symbols.Draw(e.Graphics,Symbol,ClientRectangle,Ink.IsEmpty?ForeColor:Ink);}
}
class PremiumButton : Button {
    public string Symbol="",Kind="outline";public bool Selected;bool hovered;
    public PremiumButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;DoubleBuffered=true;Cursor=Cursors.Hand;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;}
    protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?Palette.Background:Parent.BackColor);Color ink=Kind=="primary"?Color.FromArgb(5,24,30):Kind=="danger"?Palette.Red:Kind=="gold"||Kind=="link"||Selected?Palette.Gold:Kind=="teal"?Palette.Turquoise:Palette.Text;Color fill=Kind=="primary"?Palette.Turquoise:Kind=="danger"?Color.FromArgb(40,24,28):Kind=="teal"?Color.FromArgb(9,40,43):Palette.Field;bool nav=Kind=="nav"||Kind=="bare"||Kind=="link";if(!Enabled){ink=Palette.Tint(ink,Palette.Card,.48f);fill=Palette.Tint(fill,Palette.Card,.35f);}if(hovered&&Enabled&&!nav)fill=Palette.Tint(fill,Color.White,.07f);if(!nav)using(var path=RoundedPanel.Shape(ClientRectangle,Math.Max(6,(int)(Height*.16f))))using(var brush=new SolidBrush(fill))using(var pen=new Pen(Kind=="primary"?Palette.Turquoise:Kind=="danger"?Palette.Red:Kind=="gold"?Palette.Gold:Kind=="teal"?Palette.Turquoise:Palette.Muted)){g.FillPath(brush,path);g.DrawPath(pen,path);}if(Kind=="nav"&&Selected)using(var pen=new Pen(Palette.Gold,3)){g.DrawLine(pen,4,Height-3,Width-4,Height-3);}int icon=String.IsNullOrEmpty(Symbol)?0:Math.Max(16,(int)(Font.SizeInPoints*1.6));Size text=TextRenderer.MeasureText(Text,Font,new Size(1000,100),TextFormatFlags.NoPadding);int gap=icon==0||Text.Length==0?0:Math.Max(8,icon/2);int total=icon+gap+text.Width;int left=Kind=="link"?10:Math.Max(8,(Width-total)/2);if(icon>0)Symbols.Draw(g,Symbol,new RectangleF(left,(Height-icon)/2,icon,icon),ink);var bounds=new Rectangle(left+icon+gap,0,Math.Max(0,Width-left-icon-gap-5),Height);TextRenderer.DrawText(g,Text,Font,bounds,ink,TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);if(Focused&&ShowFocusCues){var r=ClientRectangle;r.Inflate(-5,-5);ControlPaint.DrawFocusRectangle(g,r,ink,fill);}}
}
class PremiumComboBox : ComboBox {
    public PremiumComboBox(){DropDownStyle=ComboBoxStyle.DropDownList;FlatStyle=FlatStyle.Flat;DrawMode=DrawMode.OwnerDrawFixed;IntegralHeight=false;MaxDropDownItems=12;DrawItem+=PaintItem;}
    void PaintItem(object sender,DrawItemEventArgs e){Color fill=(e.State&DrawItemState.Selected)!=0?Palette.Tint(Palette.Card,Palette.Gold,.14f):Palette.Field;using(var brush=new SolidBrush(fill))e.Graphics.FillRectangle(brush,e.Bounds);if(e.Index>=0)TextRenderer.DrawText(e.Graphics,Items[e.Index].ToString(),Font,e.Bounds,Palette.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
    protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width>0&&Height>0)using(var shape=RoundedPanel.Shape(ClientRectangle,9)){var previous=Region;Region=new Region(shape);if(previous!=null)previous.Dispose();}}
    void PaintClosed(Graphics g){if(Width<1||Height<1)return;using(var fill=new SolidBrush(Palette.Field))g.FillRectangle(fill,ClientRectangle);g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=RoundedPanel.Shape(ClientRectangle,9))using(var brush=new SolidBrush(Palette.Field))using(var pen=new Pen(Palette.Muted)){g.FillPath(brush,path);g.DrawPath(pen,path);}TextRenderer.DrawText(g,Text,Font,new Rectangle(18,0,Math.Max(1,Width-57),Height),Enabled?Palette.Text:Palette.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);Symbols.Draw(g,"chevron",new RectangleF(Width-35,(Height-18)/2,18,18),Palette.Muted);}
    protected override void WndProc(ref Message m){if(m.Msg==0x85){m.Result=IntPtr.Zero;return;}base.WndProc(ref m);if(m.Msg==15){using(var g=Graphics.FromHwnd(Handle))PaintClosed(g);}else if((m.Msg==0x318||m.Msg==0x317)&&m.WParam!=IntPtr.Zero){using(var g=Graphics.FromHdc(m.WParam))PaintClosed(g);}}
}
class PremiumToggle : CheckBox {
    public PremiumToggle(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;AutoSize=false;BackColor=Color.Transparent;}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;int h=Math.Min(34,Height-8),w=h*2;var track=new Rectangle(0,(Height-h)/2,w,h);using(var path=RoundedPanel.Shape(track,h/2))using(var brush=new SolidBrush(Checked?Palette.Turquoise:Palette.Border))g.FillPath(brush,path);using(var brush=new SolidBrush(Color.White)){int d=h-7;g.FillEllipse(brush,Checked?w-d-4:4,(Height-d)/2,d,d);}TextRenderer.DrawText(g,Text,Font,new Rectangle(w+16,0,Math.Max(0,Width-w-16),Height),ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
}
class Swatch : Button {
    public Color ColorValue;public bool Active;
    public Swatch(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;DoubleBuffered=true;Cursor=Cursors.Hand;}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=RoundedPanel.Shape(ClientRectangle,10))using(var brush=new LinearGradientBrush(ClientRectangle,Palette.Tint(ColorValue,Color.White,.1f),Palette.Tint(ColorValue,Color.Black,.15f),50f))using(var pen=new Pen(Active?Color.FromArgb(255,235,174):Palette.Tint(ColorValue,Color.White,.2f),Active?3:1)){e.Graphics.FillPath(brush,path);e.Graphics.DrawPath(pen,path);}if(Active)Symbols.Draw(e.Graphics,"check",new RectangleF(Width*.3f,Height*.3f,Width*.4f,Height*.4f),Color.FromArgb(30,34,33));}
}
class PremiumBrand : Control {
    public PremiumBrand(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())using(var white=new SolidBrush(Palette.Text))using(var gold=new SolidBrush(Palette.Gold)){float y=(Height-Font.GetHeight(e.Graphics))/2;float left=e.Graphics.MeasureString("Niyazi",Font,1000,format).Width;e.Graphics.DrawString("Niyazi",Font,white,0,y,format);e.Graphics.DrawString("SUGAR",Font,gold,left,y,format);}}

}
class PremiumBadge : Label {
    public PremiumBadge(){DoubleBuffered=true;}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var shape=RoundedPanel.Shape(ClientRectangle,10))using(var brush=new SolidBrush(Color.FromArgb(45,36,18)))using(var pen=new Pen(Palette.Tint(Palette.Field,Palette.Gold,.55f))){e.Graphics.FillPath(brush,shape);e.Graphics.DrawPath(pen,shape);}int icon=Math.Max(16,Height/2);Symbols.Draw(e.Graphics,"help",new RectangleF(14,(Height-icon)/2,icon,icon),Palette.Gold);TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(icon+28,0,Math.Max(1,Width-icon-34),Height),Palette.Gold,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);}
}

class PremiumHelpLabel : Label {
    public PremiumHelpLabel(){DoubleBuffered=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);int width=TextRenderer.MeasureText(Text,Font,new Size(1000,100),TextFormatFlags.NoPadding).Width;int size=Math.Max(16,(int)(Font.SizeInPoints*1.1f));Symbols.Draw(e.Graphics,"help",new RectangleF(width+12,(Height-size)/2,size,size),Palette.Muted);}
}
class PremiumSearchBox : TextBox {
    void PaintHint(Graphics graphics){if(TextLength==0)TextRenderer.DrawText(graphics,"Loglarda ara...",Font,ClientRectangle,Palette.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);}
    protected override void WndProc(ref Message message){base.WndProc(ref message);if(message.Msg==15){using(var graphics=Graphics.FromHwnd(Handle))PaintHint(graphics);}else if((message.Msg==0x317||message.Msg==0x318)&&message.WParam!=IntPtr.Zero){using(var graphics=Graphics.FromHdc(message.WParam))PaintHint(graphics);}}
}
