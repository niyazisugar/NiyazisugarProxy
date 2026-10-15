using System;
using System.Drawing;
static class WindowGeometry {
    public static Size Restore(int width,int height,Size workArea){
        int maxWidth=Math.Max(640,workArea.Width-40),maxHeight=Math.Max(460,workArea.Height-40);
        return new Size(Math.Max(640,Math.Min(width>0?width:740,maxWidth)),Math.Max(460,Math.Min(height>0?height:530,maxHeight)));
    }
    // Standard WM_NCHITTEST resize edges, including signed coordinates on secondary displays.
    public static int HitTest(Point point,Size size,int edge){
        bool left=point.X<edge,right=point.X>=size.Width-edge,top=point.Y<edge,bottom=point.Y>=size.Height-edge;
        if(top)return left?13:right?14:12;if(bottom)return left?16:right?17:15;return left?10:right?11:1;
    }
}
