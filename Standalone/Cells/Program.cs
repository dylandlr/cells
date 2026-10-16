using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace CellsStandalone;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--smoke-test"))
        {
            var sim = new MicroWorld(631);
            for (int i = 0; i < 500; i++) sim.Tick();
            if (sim.Ticks != 500 || sim.Cells.Count == 0 ||
                !double.IsFinite(sim.TotalEnergy) ||
                sim.Cells.Any(c => !double.IsFinite(c.X + c.Y + c.Energy) ||
                    c.Genes.Speed < .3 || c.Genes.Speed > 1.8 ||
                    c.Genes.Uptake < .3 || c.Genes.Uptake > 1.8))
                Environment.Exit(2);
            // Verify heritable genomes mutate within their declared bounds.
            var parent = new Genome { Speed=1, Sensing=1, Uptake=1,
                ToxinResistance=1, Signaling=1 };
            var rng = new Random(101);
            bool observedMutation = false;
            for (int i=0;i<100;i++)
            {
                var child = parent.Inherit(rng);
                if (child.Speed != parent.Speed) observedMutation = true;
                if (child.Speed < .3 || child.Speed > 1.8 ||
                    child.Sensing < .3 || child.Sensing > 1.8 ||
                    child.ToxinResistance < .3 || child.ToxinResistance > 1.8)
                    Environment.Exit(3);
            }
            if (!observedMutation) Environment.Exit(4);
            Console.WriteLine("PASS: standalone cells 500-tick deterministic smoke test");
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MicroscopeWindow());
    }
}

internal sealed class Genome
{
    public double Speed, Sensing, Uptake, ToxinResistance, Signaling;
    public Genome Copy() => new Genome { Speed=Speed, Sensing=Sensing, Uptake=Uptake,
        ToxinResistance=ToxinResistance, Signaling=Signaling };
    public static Genome Random(Random rng) => new Genome {
        Speed=.65+rng.NextDouble()*.7, Sensing=.65+rng.NextDouble()*.7,
        Uptake=.65+rng.NextDouble()*.7, ToxinResistance=.65+rng.NextDouble()*.7,
        Signaling=.65+rng.NextDouble()*.7 };
    public Genome Inherit(Random rng)
    {
        var g=Copy();
        double Mutate(double value) => Math.Clamp(
            value + (rng.NextDouble()-.5)*.17, .3, 1.8);
        g.Speed=Mutate(g.Speed); g.Sensing=Mutate(g.Sensing);
        g.Uptake=Mutate(g.Uptake); g.ToxinResistance=Mutate(g.ToxinResistance);
        g.Signaling=Mutate(g.Signaling);
        return g;
    }
}

internal sealed class CellAgent
{
    public double X, Y, Direction, Energy, Age;
    public int Id, ParentId, Generation;
    public Genome Genes = new();
    public double VelocityX, VelocityY;
    public bool Signaler => Genes.Signaling >= 1.02;
    public double SignalTimer;
    public Color Tint => Color.FromArgb(
        (int)Math.Clamp(70+Genes.Signaling*105,0,255),
        (int)Math.Clamp(135+Genes.Uptake*80,0,255),
        (int)Math.Clamp(125+Genes.ToxinResistance*73,0,255));
}

internal sealed class MicroWorld
{
    public const int Width = 76;
    public const int Height = 48;
    public readonly List<CellAgent> Cells = new();
    private double[,] food = new double[Width, Height];
    private double[,] toxin = new double[Width, Height];
    private double[,] signal = new double[Width, Height];
    private readonly Random random;
    private int nextId;
    public int Ticks { get; private set; }
    public int Births { get; private set; }
    public int Deaths { get; private set; }
    public int Signals { get; private set; }
    public int MaximumGeneration { get; private set; }
    public double MeanSpeed => Cells.Count==0?0:Cells.Average(c=>c.Genes.Speed);
    public double MeanResistance => Cells.Count==0?0:Cells.Average(c=>c.Genes.ToxinResistance);
    public double TotalEnergy => Cells.Sum(c => c.Energy);
    public double FoodAt(int x, int y) => food[Math.Clamp(x,0,Width-1),Math.Clamp(y,0,Height-1)];
    public double ToxinAt(int x, int y) => toxin[Math.Clamp(x,0,Width-1),Math.Clamp(y,0,Height-1)];
    public double SignalAt(int x, int y) => signal[Math.Clamp(x,0,Width-1),Math.Clamp(y,0,Height-1)];

    public MicroWorld(int seed)
    {
        random = new Random(seed);
        for (int i = 0; i < 18; i++)
            Cells.Add(NewCell(10 + random.NextDouble() * (Width - 20),
                7 + random.NextDouble() * (Height - 14), Genome.Random(random), 6));
        for (int i = 0; i < 9; i++)
            AddFood(random.Next(5,Width-5),random.Next(5,Height-5),6);
        for (int i = 0; i < 4; i++)
            AddToxin(random.Next(5,Width-5),random.Next(5,Height-5),3);
    }

    private CellAgent NewCell(double x, double y, Genome genes, double energy,
        int parentId=0, int generation=0) =>
        new CellAgent { Id=++nextId, ParentId=parentId, Generation=generation,
            Genes=genes, X=x, Y=y, Direction=random.NextDouble()*2*Math.PI,
            Energy=energy, SignalTimer=random.NextDouble()*4 };

    private static void Deposit(double[,] grid, double x, double y, double amount)
    {
        for (int dy=-2; dy<=2; dy++)
        for (int dx=-2; dx<=2; dx++)
        {
            int ix=(int)x+dx, iy=(int)y+dy;
            if (ix<1 || iy<1 || ix>=Width-1 || iy>=Height-1) continue;
            grid[ix,iy]+=amount*Math.Exp(-(dx*dx+dy*dy)/2.4)/8;
        }
    }
    public void AddFood(double x,double y,double amount=4) => Deposit(food,x,y,amount);
    public void AddToxin(double x,double y,double amount=3) => Deposit(toxin,x,y,amount);

    private static double[,] Diffuse(double[,] src, double rate, double decay)
    {
        var next=new double[Width,Height];
        for(int x=0;x<Width;x++)
        for(int y=0;y<Height;y++)
        {
            double a=src[x,y];
            double l=src[Math.Max(0,x-1),y];
            double r=src[Math.Min(Width-1,x+1),y];
            double b=src[x,Math.Max(0,y-1)];
            double t=src[x,Math.Min(Height-1,y+1)];
            next[x,y]=Math.Max(0,(a+rate*(l+r+b+t-4*a))*(1-decay));
        }
        return next;
    }

    private double Sample(double[,] source,double x,double y) =>
        source[Math.Clamp((int)x,0,Width-1),Math.Clamp((int)y,0,Height-1)];
    private double GradX(double[,] source,double x,double y) => Sample(source,x+1,y)-Sample(source,x-1,y);
    private double GradY(double[,] source,double x,double y) => Sample(source,x,y+1)-Sample(source,x,y-1);

    public void Tick()
    {
        Ticks++;
        food=Diffuse(food,.11,.00008);
        toxin=Diffuse(toxin,.19,.003);
        signal=Diffuse(signal,.22,.014);

        if(Ticks%130==0)
            AddFood(random.Next(6,Width-6),random.Next(6,Height-6),4.5);

        var newborn=new List<CellAgent>();
        for(int index=Cells.Count-1;index>=0;index--)
        {
            var c=Cells[index];
            c.Age+=.04;
            c.SignalTimer-=.04;
            int ix=Math.Clamp((int)c.X,0,Width-1), iy=Math.Clamp((int)c.Y,0,Height-1);
            double localFood=food[ix,iy], localToxin=toxin[ix,iy];
            double eaten=Math.Min(localFood,.018*c.Genes.Uptake);
            food[ix,iy]-=eaten;
            c.Energy+=eaten*7-(.005+.003*c.Genes.Speed*c.Genes.Speed)-localToxin*.003/c.Genes.ToxinResistance;
            if(c.Signaler && eaten>.005 && c.SignalTimer<=0 && c.Energy>.8)
            {
                Deposit(signal,c.X,c.Y,.20*c.Genes.Signaling);
                c.Energy-=.09*c.Genes.Signaling;
                c.SignalTimer=1.4;
                Signals++;
            }
            double gx=(GradX(food,c.X,c.Y)+GradX(signal,c.X,c.Y)*.55-GradX(toxin,c.X,c.Y)*1.4)*c.Genes.Sensing;
            double gy=(GradY(food,c.X,c.Y)+GradY(signal,c.X,c.Y)*.55-GradY(toxin,c.X,c.Y)*1.4)*c.Genes.Sensing;
            double angle=Math.Atan2(gy,gx);
            double strength=Math.Sqrt(gx*gx+gy*gy);
            if(strength>.00004)
            {
                double delta=Math.Atan2(Math.Sin(angle-c.Direction),Math.Cos(angle-c.Direction));
                c.Direction+=Math.Clamp(delta,-.08*c.Genes.Sensing,.08*c.Genes.Sensing);
            }
            c.Direction+=(random.NextDouble()-.5)*.23;
            // Damped momentum produces curved swimming paths instead of direct jumps.
            double thrust=(.045+Math.Min(.07,Math.Sqrt(strength)*.3))*c.Genes.Speed;
            c.VelocityX=c.VelocityX*.88+Math.Cos(c.Direction)*thrust;
            c.VelocityY=c.VelocityY*.88+Math.Sin(c.Direction)*thrust;
            double motion=Math.Sqrt(c.VelocityX*c.VelocityX+c.VelocityY*c.VelocityY);
            double maxMotion=.35*c.Genes.Speed;
            if(motion>maxMotion)
            {
                c.VelocityX*=maxMotion/motion; c.VelocityY*=maxMotion/motion;
                motion=maxMotion;
            }
            double nextX=Math.Clamp(c.X+c.VelocityX,1,Width-2);
            double nextY=Math.Clamp(c.Y+c.VelocityY,1,Height-2);
            if(nextX!=c.X+c.VelocityX)c.VelocityX*=-.2;
            if(nextY!=c.Y+c.VelocityY)c.VelocityY*=-.2;
            c.X=nextX; c.Y=nextY;
            c.Energy-=motion*.007*c.Genes.Speed;
            if(c.Energy<=0 || c.Age>210)
            {
                Cells.RemoveAt(index);
                Deaths++;
            }
            else if(c.Energy>11 && Cells.Count+newborn.Count<100 && c.Age>4)
            {
                c.Energy*=.5;
                var child=NewCell(Math.Clamp(c.X+.5,1,Width-2),
                    Math.Clamp(c.Y+.5,1,Height-2),
                    c.Genes.Inherit(random),c.Energy,c.Id,c.Generation+1);
                newborn.Add(child);
                MaximumGeneration=Math.Max(MaximumGeneration,child.Generation);
                Births++;
            }
        }
        Cells.AddRange(newborn);
    }
}

internal sealed class MicroscopeWindow : Form
{
    private MicroWorld world=new(631);
    private readonly System.Windows.Forms.Timer timer = new() { Interval=33 };
    private bool paused;
    private bool showChemistry=true;
    private int speed=1;
    private int seed=631;
    private string tool="FOOD";
    private int selectedId;
    private CellAgent? Selected => world.Cells.FirstOrDefault(c=>c.Id==selectedId);
    private Point mouse=Point.Empty;
    private const int UiHeight=106;
    private readonly Font uiFont=new("Segoe UI",10,FontStyle.Regular);
    private readonly Font titleFont=new("Segoe UI Semibold",17,FontStyle.Bold);
    private readonly Font monoFont=new("Consolas",9,FontStyle.Regular);

    public MicroscopeWindow()
    {
        Text="cells  /  microscopic ecology prototype";
        MinimumSize=new Size(850,600);
        ClientSize=new Size(1150,780);
        BackColor=Color.FromArgb(6,16,24);
        ForeColor=Color.FromArgb(230,246,242);
        DoubleBuffered=true;
        KeyPreview=true;
        StartPosition=FormStartPosition.CenterScreen;
        timer.Tick+=(s,e)=>{
            if(!paused){for(int i=0;i<speed;i++)world.Tick();}
            Invalidate();
        };
        timer.Start();
        KeyDown+=(s,e)=>{
            switch(e.KeyCode)
            {
                case Keys.Space:paused=!paused;break;
                case Keys.R:world=new MicroWorld(++seed);selectedId=0;paused=false;break;
                case Keys.D1:speed=1;break;
                case Keys.D2:speed=3;break;
                case Keys.D3:speed=10;break;
                case Keys.F:tool="FOOD";break;
                case Keys.T:tool="TOXIN";break;
                case Keys.C:showChemistry=!showChemistry;break;
                case Keys.I:selectedId=0;break;
                case Keys.Escape:Close();break;
            }
        };
        MouseMove+=(s,e)=>{mouse=e.Location;};
        MouseDown+=(s,e)=>{
            var area=WorldRect();
            if(!area.Contains(e.Location))return;
            double x=(e.X-area.Left)*MicroWorld.Width/(double)area.Width;
            double y=(e.Y-area.Top)*MicroWorld.Height/(double)area.Height;
            // Left clicking a cell selects it; clicking elsewhere edits the environment.
            var nearby=world.Cells.OrderBy(c=>(c.X-x)*(c.X-x)+(c.Y-y)*(c.Y-y)).FirstOrDefault();
            if(e.Button==MouseButtons.Left && nearby!=null &&
                (nearby.X-x)*(nearby.X-x)+(nearby.Y-y)*(nearby.Y-y)<2.25)
            {
                selectedId=nearby.Id;
                return;
            }
            if(e.Button==MouseButtons.Right || tool=="TOXIN")world.AddToxin(x,y);
            else world.AddFood(x,y);
        };
    }

    private Rectangle WorldRect()
    {
        int left=24, top=UiHeight+14, availableW=ClientSize.Width-48;
        int availableH=ClientSize.Height-top-126;
        double ratio=(double)MicroWorld.Width/MicroWorld.Height;
        int w=Math.Min(availableW,(int)(availableH*ratio));
        int h=(int)(w/ratio);
        return new Rectangle(left+(availableW-w)/2,top,w,h);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g=e.Graphics;
        g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        using var heading=new SolidBrush(Color.FromArgb(179,245,226));
        using var muted=new SolidBrush(Color.FromArgb(139,171,180));
        g.DrawString("cells",titleFont,heading,24,14);
        g.DrawString("MICROSCOPE  /  EVOLVING MICROBIOME  •  v0.3",monoFont,muted,120,25);
        string stats=$"t = {world.Ticks*.04:0.0}s    alive {world.Cells.Count}    births {world.Births}    deaths {world.Deaths}    generations {world.MaximumGeneration}    pulses {world.Signals}    total ATP {world.TotalEnergy:0.0}";
        g.DrawString(stats,uiFont,heading,26,56);
        g.DrawString($"TRAITS  swimming {world.MeanSpeed:0.00}  /  toxin defense {world.MeanResistance:0.00}    •    Green nutrients  /  Magenta toxins  /  Gold signals", monoFont,muted,26,82);
        var rect=WorldRect();
        MicroscopeArt.DrawEnvironment(g,rect,world,showChemistry);
        foreach(var c in world.Cells)
            MicroscopeArt.DrawCell(g,c,rect,world.Ticks,c.Id==selectedId);
        using var border=new Pen(Color.FromArgb(65,139,151),1f);
        g.DrawRectangle(border,rect);
        string pausedText=paused?"PAUSED":"RUNNING";
        string footer=$"SPACE {pausedText}    R RESEED    1/2/3 SPEED {speed}×    F FOOD    T TOXIN    C CHEMICAL MAP    CLICK CELL INSPECT    CLICK EMPTY PLACE {tool}    RIGHT CLICK TOXIN";
        g.DrawString(footer,monoFont,heading,rect.Left,rect.Bottom+14);
        var selected=Selected;
        string inspector=selected==null
            ? $"EVOLUTION  mean speed {world.MeanSpeed:0.00}   mean toxin resistance {world.MeanResistance:0.00}    |    Select a cell to inspect its genome"
            : $"CELL #{selected.Id}  parent #{selected.ParentId}  gen {selected.Generation}  ATP {selected.Energy:0.0}  age {selected.Age:0.0}  " +
              $"speed {selected.Genes.Speed:0.00}  sense {selected.Genes.Sensing:0.00}  uptake {selected.Genes.Uptake:0.00}  toxin defense {selected.Genes.ToxinResistance:0.00}  signal {selected.Genes.Signaling:0.00}";
        g.DrawString(inspector,monoFont,heading,rect.Left,rect.Bottom+34);
        g.DrawString("Simplified, uncalibrated Windows ecology prototype. Genes mutate at reproduction; not the Unity biology engine.",
            monoFont,muted,rect.Left,rect.Bottom+55);
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing){timer.Dispose();uiFont.Dispose();titleFont.Dispose();monoFont.Dispose();}
        base.Dispose(disposing);
    }
}
