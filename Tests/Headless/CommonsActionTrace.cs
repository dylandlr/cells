using System;
using System.Globalization;
using System.Text;
using Cells.Experiments;

internal static class CommonsActionTrace
{
    // CSV is intentionally simple: the viewer receives simulation facts, not its own model.
    public static string Generate(int seed=1010000,int steps=60)
    {
        if(steps<1||steps>1000)throw new ArgumentOutOfRangeException("steps");
        var env=new RenewableCommonsV0(seed,true);
        var csv=new StringBuilder("tick,kind,agent,patch,amount,energy,stock,detail\n");
        for(int tick=0;tick<steps;tick++)
        {
            var acts=new RenewableCommonsV0.Act[4];
            var before=new RenewableCommonsV0.View[4];
            for(int i=0;i<4;i++)before[i]=env.Observe(i);
            // Explicit scripted demonstration policy; not a learned agent.
            for(int i=0;i<4;i++)
            {
                var v=before[i];
                int a=!v.Alive?9:
                    i==0 && tick%4==0 && v.LocalStock<=2?6+v.Location:
                    v.LocalStock<=2?3+v.Location:v.Location;
                if(i>0 && v.Alive)
                {
                    int invitation=-1;
                    for(int p=0;p<3;p++)if(v.Heard[p]>0){invitation=p;break;}
                    a=invitation>=0?3+invitation:v.Location;
                }
                acts[i]=new RenewableCommonsV0.Act(a);
            }
            var log=env.Advance(acts);
            var after=env.Stocks();
            for(int i=0;i<4;i++)
            {
                int a=acts[i].Code;
                int patch=a<9?a%3:-1;
                string label=a<3?"harvest":a<6?"restore-attempt":a<9?"signal":"rest";
                Add(csv,tick,label,i,patch,log.Rewards[i],env.Observe(i).Energy,patch>=0?after[patch]:-1,before[i].Alive?"acted":"inactive");
                if(before[i].Alive && !env.Observe(i).Alive)
                    Add(csv,tick,"death",i,before[i].Location,0,env.Observe(i).Energy,-1,"energy-depleted");
            }
            // Stock changes alone can also include background replenishment.
            for(int p=0;p<3;p++)
                Add(csv,tick,"stock", -1,p,after[p]-log.StocksBefore[p],0,after[p],"post-step-observation");
            if(log.SuccessfulRepairs>0)
                Add(csv,tick,"repair-success",-1,-1,log.SuccessfulRepairs,0,env.SubstrateReserve,"joint-restoration-events");
        }
        return csv.ToString();
    }
    private static void Add(StringBuilder s,int tick,string kind,int agent,int patch,
        double amount,double energy,int stock,string detail)
    {
        s.Append(tick).Append(',').Append(kind).Append(',').Append(agent).Append(',')
          .Append(patch).Append(',').Append(amount.ToString("R",CultureInfo.InvariantCulture))
          .Append(',').Append(energy.ToString("R",CultureInfo.InvariantCulture))
          .Append(',').Append(stock).Append(',').Append(detail).Append('\n');
    }
}
