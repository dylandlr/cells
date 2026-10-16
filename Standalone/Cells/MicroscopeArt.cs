using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CellsStandalone;

/// <summary>
/// Procedural microscope appearance tied to inherited phenotypes.
/// Visual anatomy is illustrative, not literal biophysical organelle simulation.
/// </summary>
internal static class MicroscopeArt
{
    public static void DrawEnvironment(Graphics g, Rectangle rect, MicroWorld world, bool chemistry)
    {
        using var background = new SolidBrush(Color.FromArgb(7, 18, 28));
        g.FillRectangle(background, rect);
        float wx = rect.Width / (float)MicroWorld.Width;
        float wy = rect.Height / (float)MicroWorld.Height;
        using var tile = new SolidBrush(Color.Black);
        for (int x=0; x<MicroWorld.Width; x++)
        for (int y=0; y<MicroWorld.Height; y++)
        {
            double n = world.FoodAt(x,y), t=world.ToxinAt(x,y), s=world.SignalAt(x,y);
            int r=(int)Math.Clamp(8+t*290+s*95,0,255);
            int green=(int)Math.Clamp(21+n*320+s*62,0,255);
            int blue=(int)Math.Clamp(31+t*130+s*220,0,255);
            tile.Color = chemistry ? Color.FromArgb(r,green,blue) : Color.FromArgb(9,27,38);
            g.FillRectangle(tile,rect.Left+x*wx,rect.Top+y*wy,wx+1,wy+1);
        }
        // Optical reticle and consistent measurement grid.
        using var gridPen=new Pen(Color.FromArgb(17,104,146,154),1);
        for(int x=0;x<=MicroWorld.Width;x+=8)
        {
            float px=rect.Left+x*wx;
            g.DrawLine(gridPen,px,rect.Top,px,rect.Bottom);
        }
        for(int y=0;y<=MicroWorld.Height;y+=8)
        {
            float py=rect.Top+y*wy;
            g.DrawLine(gridPen,rect.Left,py,rect.Right,py);
        }
        using var centerPen=new Pen(Color.FromArgb(44,147,199,202),1);
        float cx=rect.Left+rect.Width/2f,cy=rect.Top+rect.Height/2f;
        g.DrawEllipse(centerPen,cx-12,cy-12,24,24);
        g.DrawLine(centerPen,cx-20,cy,cx-14,cy);
        g.DrawLine(centerPen,cx+14,cy,cx+20,cy);
        g.DrawLine(centerPen,cx,cy-20,cx,cy-14);
        g.DrawLine(centerPen,cx,cy+14,cx,cy+20);
    }

    public static void DrawCell(Graphics g, CellAgent cell, Rectangle rect, int tick, bool selected)
    {
        float scale = rect.Width/(float)MicroWorld.Width;
        float x=rect.Left+(float)cell.X*scale;
        float y=rect.Top+(float)cell.Y*(rect.Height/(float)MicroWorld.Height);
        float radius=Math.Max(4.5f,scale*(float)(.40+.12*cell.Genes.Uptake));
        float angle=(float)Math.Atan2(cell.VelocityY,cell.VelocityX);
        if(Math.Abs(cell.VelocityX)+Math.Abs(cell.VelocityY)<.01)angle=(float)cell.Direction;

        var state=g.Save();
        g.TranslateTransform(x,y);
        g.RotateTransform(angle*180/(float)Math.PI);
        float bodyL=radius*(float)(1.0+Math.Clamp(cell.Genes.Speed-.75,0,.75)*.42);
        float bodyH=radius*(float)(.86+Math.Clamp(cell.Genes.ToxinResistance-.5,0,1)*.2);
        float wave=(float)Math.Sin(tick*.24+cell.Id*1.27);
        Color tint=cell.Tint;
        using var halo=new SolidBrush(Color.FromArgb(22,tint));
        g.FillEllipse(halo,-bodyL*1.5f,-bodyH*1.6f,bodyL*3,bodyH*3.2f);

        // Flagellum: curved animated attachment follows propulsion direction.
        using var tailPen=new Pen(Color.FromArgb(175,tint),Math.Max(.75f,radius*.15f));
        tailPen.StartCap=LineCap.Round; tailPen.EndCap=LineCap.Round;
        using var tail=new GraphicsPath();
        tail.AddBezier(-bodyL*.8f,0,-bodyL*1.5f,bodyH*.35f*wave,
            -bodyL*1.9f,-bodyH*.9f*wave,-bodyL*2.8f,bodyH*.50f*wave);
        g.DrawPath(tailPen,tail);

        var shape=new RectangleF(-bodyL,-bodyH,bodyL*2,bodyH*2);
        using var cellFill=new LinearGradientBrush(shape,
            Color.FromArgb(145,tint),Color.FromArgb(52,tint),65f);
        g.FillEllipse(cellFill,shape);
        using var membrane=new Pen(Color.FromArgb(226,tint),Math.Max(1.2f,radius*.14f));
        g.DrawEllipse(membrane,shape);

        // Energy stores and vacuoles are visual cues based on current energy.
        using var cytoplasm=new SolidBrush(Color.FromArgb(95,8,27,42));
        g.FillEllipse(cytoplasm,-bodyL*.50f,-bodyH*.43f,bodyL*1.0f,bodyH*.86f);
        using var nucleus=new SolidBrush(Color.FromArgb(225,225,244,210));
        g.FillEllipse(nucleus,-bodyL*.10f,-bodyH*.29f,bodyL*.58f,bodyH*.58f);
        using var granule=new SolidBrush(Color.FromArgb(200,255,204,113));
        int granules=(int)Math.Clamp(cell.Energy/2,0,5);
        for(int i=0;i<granules;i++)
        {
            float t=i*2.39996f+cell.Id*.13f;
            float dx=(float)Math.Cos(t)*bodyL*.50f,dy=(float)Math.Sin(t)*bodyH*.48f;
            float sz=Math.Max(1.0f,radius*.17f);
            g.FillEllipse(granule,dx,dy,sz,sz);
        }
        // A signaling-specialist phenotype has a distinct anterior receptor marking.
        if(cell.Signaler)
        {
            using var chemoreceptor=new SolidBrush(Color.FromArgb(240,255,196,83));
            g.FillEllipse(chemoreceptor,bodyL*.54f,-bodyH*.15f,Math.Max(2f,radius*.31f),Math.Max(2f,radius*.31f));
        }
        if(selected)
        {
            using var selectionPen=new Pen(Color.FromArgb(225,250,251,232),1.3f){DashStyle=DashStyle.Dash};
            g.DrawEllipse(selectionPen,-bodyL*1.65f,-bodyH*1.65f,bodyL*3.3f,bodyH*3.3f);
        }
        g.Restore(state);
    }
}
