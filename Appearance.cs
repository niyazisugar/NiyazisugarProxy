using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
class RoundedPanel : Panel {
    public Color Surface=Color.FromArgb(25,30,43),Border=Color.FromArgb(35,42,59);
    public RoundedPanel(){DoubleBuffered=true;}
    public static GraphicsPath Shape(Rectangle r,int radius){var p=new GraphicsPath();int d=Math.Min(radius*2,Math.Min(r.Width,r.Height));if(d<=0)return p;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d-1,r.Y,d,d,270,90);p.AddArc(r.Right-d-1,r.Bottom-d-1,d,d,0,90);p.AddArc(r.X,r.Bottom-d-1,d,d,90,90);p.CloseFigure();return p;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var shape=Shape(ClientRectangle,16))using(var brush=new SolidBrush(Surface))using(var pen=new Pen(Border)){e.Graphics.FillPath(brush,shape);e.Graphics.DrawPath(pen,shape);}}
    protected override void OnResize(EventArgs e){base.OnResize(e);using(var shape=Shape(ClientRectangle,16)){var old=Region;Region=new Region(shape);if(old!=null)old.Dispose();}}
}
class RoundedButton : Button {
    public RoundedButton(){DoubleBuffered=true;Cursor=Cursors.Hand;}
    protected override void OnResize(EventArgs e){base.OnResize(e);using(var shape=RoundedPanel.Shape(ClientRectangle,10)){var old=Region;Region=new Region(shape);if(old!=null)old.Dispose();}}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Color color=Enabled?BackColor:Color.FromArgb(65,70,85);using(var shape=RoundedPanel.Shape(ClientRectangle,10))using(var brush=new SolidBrush(color))e.Graphics.FillPath(brush,shape);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?ForeColor:Color.FromArgb(150,155,169),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);if(Focused){Rectangle r=ClientRectangle;r.Inflate(-4,-4);ControlPaint.DrawFocusRectangle(e.Graphics,r,ForeColor,color);}}
}
