using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CellsStandalone;

/// <summary>Illustrative phenotype-linked microscope rendering, not calibrated cell anatomy.</summary>
internal static class MicroscopeArt
{
    private static float Noise(int x, int y)
    {
        unchecked
        {
            uint v=(uint)(x*374761393+y*668265263);
            v=(v^(v>>13))*1274126177u;
            return (v^(v>>16))/4294967295f;
        }
    }

    public static void DrawEnvironment(Graphics g, Rectangle rect, MicroWorld world, bool chemistry)
    {
        using var backdrop=new LinearGradientBrush(rect,
            Color.FromArgb(7,24,34),Color.FromArgb(3,11,21),LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(backdrop,rect);
        var clip=g.Save();
        g.SetClip(rect);
        float wx=rect.Width/(float)MicroWorld.Width,wy=rect.Height/(float)MicroWorld.Height;
        using var tile=new SolidBrush(Color.Black);
        for(int x=0;x<MicroWorld.Width;x++)
        for(int y=0;y<MicroWorld.Height;y++)
        {
            double n=world.FoodAt(x,y),t=world.ToxinAt(x,y),s=world.SignalAt(x,y);
            float grain=Noise(x,y)*5f;
            int red=(int)Math.Clamp(7+grain+t*265+s*90,0,255);
            int green=(int)Math.Clamp(22+grain+n*295+s*65,0,255);
            int blue=(int)Math.Clamp(32+grain+t*125+s*210,0,255);
            tile.Color=chemistry?Color.FromArgb(red,green,blue):
                Color.FromArgb(9+(int)grain,24+(int)grain,34+(int)grain);
            g.FillRectangle(tile,rect.Left+x*wx,rect.Top+y*wy,wx+1,wy+1);
        }

        // Sparse dust motes and bacteria-sized debris have deterministic positions.
        // They are purely optical texture and should not be mistaken for simulated agents.
        for(int i=0;i<185;i++)
        {
            float px=rect.Left+Noise(i,72)*rect.Width;
            float py=rect.Top+Noise(i,94)*rect.Height;
            float radius=.45f+Noise(i,86)*1.1f;
            using var mote=new SolidBrush(Color.FromArgb(25+(int)(Noise(i,99)*33),147,200,180));
            g.FillEllipse(mote,px,py,radius,radius);
        }

        // Reticle grid is deliberately subtle so chemistry remains continuous-looking.
        using var grid=new Pen(Color.FromArgb(13,130,194,194),1);
        for(int x=0;x<=MicroWorld.Width;x+=10)
        {
            float px=rect.Left+x*wx;
            g.DrawLine(grid,px,rect.Top,px,rect.Bottom);
        }
        for(int y=0;y<=MicroWorld.Height;y+=10)
        {
            float py=rect.Top+y*wy;
            g.DrawLine(grid,rect.Left,py,rect.Right,py);
        }
        using var scalePen=new Pen(Color.FromArgb(165,160,211,202),2);
        float sx=rect.Right-110,sy=rect.Bottom-22;
        g.DrawLine(scalePen,sx,sy,sx+80,sy);
        g.DrawLine(scalePen,sx,sy-4,sx,sy+4);
        g.DrawLine(scalePen,sx+80,sy-4,sx+80,sy+4);
        using var labelBrush=new SolidBrush(Color.FromArgb(175,204,228,222));
        using var labelFont=new Font("Consolas",8,FontStyle.Regular);
        g.DrawString("10 SIM UNITS",labelFont,labelBrush,sx-2,sy-18);
        g.Restore(clip);
    }

    private static GraphicsPath Body(float length,float height,int shape,float pulse)
    {
        var path=new GraphicsPath();
        if(shape==0) // Oval swimmer
            path.AddEllipse(-length,-height,length*2,height*2);
        else if(shape==1) // Elongated, tapered swimmer
        {
            path.AddBezier(-length,0,-length*.85f,-height*1.35f,length*.35f,-height*.9f,length,0);
            path.AddBezier(length,0,length*.38f,height*.85f,-length*.90f,height*1.3f,-length,0);
        }
        else // Lobed membrane
        {
            PointF[] pts=new PointF[36];
            for(int i=0;i<pts.Length;i++)
            {
                float a=i*(float)(2*Math.PI/pts.Length);
                float ripple=1f+.095f*(float)Math.Sin(a*3+pulse);
                pts[i]=new PointF(length*ripple*(float)Math.Cos(a),
                                  height*ripple*(float)Math.Sin(a));
            }
            path.AddClosedCurve(pts,.48f);
        }
        return path;
    }

    public static void DrawCell(Graphics g,CellAgent cell,Rectangle rect,int tick,bool selected)
    {
        float sw=rect.Width/(float)MicroWorld.Width;
        float sh=rect.Height/(float)MicroWorld.Height;
        float x=rect.Left+(float)cell.X*sw,y=rect.Top+(float)cell.Y*sh;
        float radius=Math.Max(4.6f,sw*(float)(.44+.14*cell.Genes.Uptake));
        float angle=(float)Math.Atan2(cell.VelocityY,cell.VelocityX);
        if(Math.Abs(cell.VelocityX)+Math.Abs(cell.VelocityY)<.01)angle=(float)cell.Direction;
        var original=g.Save();
        g.TranslateTransform(x,y);
        g.RotateTransform(angle*180f/(float)Math.PI);

        int form=(int)Math.Clamp(Math.Floor(cell.Genes.Speed*2.5),0,2);
        float length=radius*(form==1?1.48f:form==2?1.13f:1f);
        float height=radius*(form==1?.68f:1f);
        float wobble=(float)Math.Sin(tick*.19+cell.Id*.8);
        float phase=(float)Math.Sin(tick*.35+cell.Id*1.72);
        Color tint=cell.Tint;
        using var glow=new SolidBrush(Color.FromArgb(24,tint));
        g.FillEllipse(glow,-length*1.55f,-height*1.7f,length*3.1f,height*3.4f);

        // Thin undulating propulsive appendage; motility speed influences amplitude.
        using var flagellum=new Pen(Color.FromArgb(190,tint),Math.Max(1f,radius*.17f));
        flagellum.StartCap=LineCap.Round;
        using var flagPath=new GraphicsPath();
        float amplitude=height*(.50f+.25f*(float)cell.Genes.Speed);
        flagPath.AddBezier(-length*.92f,0,-length*1.5f,phase*amplitude,
            -length*2.1f,-phase*amplitude,-length*3f,phase*amplitude*.80f);
        g.DrawPath(flagellum,flagPath);
        if(form==2)
        {
            using var second=new Pen(Color.FromArgb(122,tint),Math.Max(.8f,radius*.11f));
            g.DrawBezier(second,-length*.8f,height*.35f,-length*1.3f,height*.95f,
                -length*1.8f,-height*.7f,-length*2.3f,height*.7f*phase);
        }

        using var silhouette=Body(length,height,form,wobble);
        using var membraneFill=new SolidBrush(Color.FromArgb(151,tint));
        g.FillPath(membraneFill,silhouette);
        using var lining=new Pen(Color.FromArgb(232,tint),Math.Max(1.1f,radius*.18f));
        g.DrawPath(lining,silhouette);
        using var interior=new SolidBrush(Color.FromArgb(94,9,29,45));
        g.FillEllipse(interior,-length*.61f,-height*.62f,length*1.27f,height*1.25f);
        using var vacuole=new Pen(Color.FromArgb(115,180,246,216),Math.Max(.65f,radius*.09f));
        g.DrawEllipse(vacuole,-length*.5f,-height*.25f,length*.55f,height*.6f);
        using var dna=new SolidBrush(Color.FromArgb(228,236,239,208));
        g.FillEllipse(dna,length*.08f,-height*.29f,length*.48f,height*.58f);

        using var granule=new SolidBrush(Color.FromArgb(231,250,197,111));
        int granules=(int)Math.Clamp(cell.Energy/1.9,0,6);
        for(int i=0;i<granules;i++)
        {
            float a=i*2.4f+cell.Id*.3f+tick*.016f;
            float gx=(float)Math.Cos(a)*length*.49f,gy=(float)Math.Sin(a)*height*.48f;
            float size=Math.Max(1.25f,radius*.16f);
            g.FillEllipse(granule,gx-size*.5f,gy-size*.5f,size,size);
        }

        if(cell.Signaler)
        {
            using var receptor=new SolidBrush(Color.FromArgb(244,255,206,100));
            float size=Math.Max(2f,radius*.28f);
            g.FillEllipse(receptor,length*.64f-size*.5f,-size*.5f,size,size);
        }
        if(selected)
        {
            using var outline=new Pen(Color.FromArgb(239,245,250,229),1.5f)
                {DashStyle=DashStyle.Dash};
            g.DrawEllipse(outline,-length*1.65f,-height*1.7f,length*3.3f,height*3.4f);
        }
        g.Restore(original);
    }
}
