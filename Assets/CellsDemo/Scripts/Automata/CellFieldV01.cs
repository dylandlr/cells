using System;
using System.Globalization;
using System.Text;
namespace Cells.Automata
{
    // Deterministic spatial ecological test bed: local nutrient parcels, bounded movement,
    // machine uptake and metabolism. It is not yet hydrodynamics or calibrated biology.
    public static class CellFieldV01
    {
        private sealed class Cell
        {
            public readonly int Id;
            public double X,Y,Energy=2,Internal,Integrity=.85;
            public Cell(int id,double x,double y){Id=id;X=x;Y=y;}
        }
        public static string Trace(int ticks=120)
        {
            if(ticks<1||ticks>10000)throw new ArgumentOutOfRangeException(nameof(ticks));
            const int size=20;
            var field=new double[size,size];var cells=new[]{new Cell(0,.3,.3),new Cell(1,.7,.7),new Cell(2,.3,.7),new Cell(3,.7,.3)};
            // Explicit initialization; units are arbitrary nutrient tokens.
            for(int x=0;x<size;x++)for(int y=0;y<size;y++)field[x,y]=0.5+((x+y)%5==0?1.0:0);
            double waste=0,initial=0;for(int x=0;x<size;x++)for(int y=0;y<size;y++)initial+=field[x,y];
            var sb=new StringBuilder("tick,agent,x,y,energy,integrity,internal,local_nutrient,uptake,metabolized,waste,total_material\n");
            string F(double d)=>d.ToString("0.000000",CultureInfo.InvariantCulture);
            for(int t=0;t<ticks;t++)
            {
                // Diffusion via conservative simultaneous transfers with no-flux walls.
                var next=(double[,])field.Clone();
                for(int x=0;x<size;x++)for(int y=0;y<size;y++)
                {
                    if(x+1<size){double flux=.08*(field[x,y]-field[x+1,y]);next[x,y]-=flux;next[x+1,y]+=flux;}
                    if(y+1<size){double flux=.08*(field[x,y]-field[x,y+1]);next[x,y]-=flux;next[x,y+1]+=flux;}
                }
                field=next;
                foreach(var c in cells)
                {
                    int ix=Math.Min(size-1,(int)(c.X*size)),iy=Math.Min(size-1,(int)(c.Y*size));
                    double gx=(field[Math.Min(size-1,ix+1),iy]-field[Math.Max(0,ix-1),iy]);
                    double gy=(field[ix,Math.Min(size-1,iy+1)]-field[ix,Math.Max(0,iy-1)]);
                    // Chemotaxis is an explicitly coded fixed controller, not learned intelligence.
                    double dx=.004*Math.Sign(gx),dy=.004*Math.Sign(gy);
                    double motionCost=.002*(Math.Abs(dx)+Math.Abs(dy))/.004;
                    if(c.Energy>=motionCost){c.X=Math.Max(0.02,Math.Min(.98,c.X+dx));c.Y=Math.Max(.02,Math.Min(.98,c.Y+dy));c.Energy-=motionCost;}
                    ix=Math.Min(size-1,(int)(c.X*size));iy=Math.Min(size-1,(int)(c.Y*size));
                    double sensor=field[ix,iy]/(1+field[ix,iy]);
                    double uptake=Math.Min(field[ix,iy],Math.Min(.10*c.Integrity*sensor,c.Energy/.03));
                    field[ix,iy]-=uptake;c.Internal+=uptake;c.Energy-=uptake*.03;
                    double catabolized=Math.Min(.07,c.Internal);c.Internal-=catabolized;
                    c.Energy+=catabolized*.7;
                    double byproduct=catabolized*.3;waste+=byproduct;
                    double total=c.Internal+waste;
                    foreach(var z in cells)if(z!=c)total+=z.Internal;
                    for(int x=0;x<size;x++)for(int y=0;y<size;y++)total+=field[x,y];
                    if(Math.Abs(total-initial)>1e-7)throw new InvalidOperationException("Nutrient conservation failed");
                    sb.Append(t).Append(',').Append(c.Id).Append(',').Append(F(c.X)).Append(',').Append(F(c.Y)).Append(',').Append(F(c.Energy)).Append(',').Append(F(c.Integrity)).Append(',').Append(F(c.Internal)).Append(',').Append(F(field[ix,iy])).Append(',').Append(F(uptake)).Append(',').Append(F(catabolized)).Append(',').Append(F(waste)).Append(',').Append(F(total)).Append('\n');
                }
            }
            return sb.ToString();
        }
    }
}
