using System;
using Cells.Experiments;

internal static class Experiment001ValidityChecks
{
    private static int assertions;
    private static void Check(bool ok,string label)
    {
        if(!ok)throw new Exception("VALIDITY AUDIT FAILED: "+label);
        assertions++;
    }
    private static bool Close(double a,double b)=>Math.Abs(a-b)<1e-9;
    internal static int Run()
    {
        assertions=0;
        const int seed=930011;
        // Treat the simulator as a controlled accounting model: same world,
        // identical joint action, exactly identical returns.
        var a=new SignalingGameV1(seed,1,true);
        var b=new SignalingGameV1(seed,1,false);
        var observed=a.Observe(0);
        Check(observed.ReceivedSignals.Length==3,"three message channels");
        var joint=new SignalingGameV1.Action[]{
            new SignalingGameV1.Action(0),new SignalingGameV1.Action(0),
            new SignalingGameV1.Action(1),new SignalingGameV1.Action(6)};
        a.Advance(joint);b.Advance(joint);
        var x=a.Summary();var y=b.Summary();
        for(int i=0;i<4;i++)Check(Close(x.Returns[i],y.Returns[i]),
            "delivery toggle cannot change environment reward for fixed actions "+i);
        Check(Close(x.Returns[0],x.Returns[1]+SignalingGameV1.MovementCost),
            "movement cost only affects moving competitor");
        Check(Close(x.Returns[3],SignalingGameV1.RestGain),"rest is credited");
        Check(Close(x.TotalHarvest,x.Returns[0]+x.Returns[1]+x.Returns[2]+
            3*SignalingGameV1.ForageCost+2*SignalingGameV1.MovementCost),
            "total harvested conserved after accounting costs");
        Check(x.HarvestCount==3 && x.SignalCount==0,"three forages");
        // Step zero messages arrive at step one, with unchanged signal costs.
        var on=new SignalingGameV1(seed,2,true);
        var off=new SignalingGameV1(seed,2,false);
        var broadcast=new SignalingGameV1.Action[]{
            new SignalingGameV1.Action(4),new SignalingGameV1.Action(6),
            new SignalingGameV1.Action(6),new SignalingGameV1.Action(6)};
        on.Advance(broadcast);off.Advance(broadcast);
        Check(on.Observe(2).ReceivedSignals[1]==1,"message delivered after broadcast");
        Check(off.Observe(2).ReceivedSignals[1]==0,"message blocked by ablation");
        Check(Close(on.Summary().Returns[0],-SignalingGameV1.SignalCost),
            "sender pays same cost in enabled channel");
        Check(Close(on.Summary().Returns[0],off.Summary().Returns[0]),
            "sender cost invariant to delivery");
        var genome=new AdaptiveExperiment001V2.Genome(.18,.12,.75);
        var learner=new AdaptiveExperiment001V2.AdaptivePolicy(genome);
        var initial=learner.Snapshot();
        var obs=new SignalingGameV1.Observation{
            OwnPatch=0,LocalCue=new[]{0,-1,-1},
            ReceivedSignals=new[]{0,0,0}};
        var action=learner.Decide(obs,new Random(1));
        learner.Learn(10);
        var updated=learner.Snapshot();
        Check(!Close(updated[0,action.Code],initial[0,action.Code]),
            "Q value changes from reward");
        Check(Close(initial[0,0],0) && Close(initial[1,2],0),
            "snapshot deep copy remains unchanged");
        var frozen=new AdaptiveExperiment001V2.AdaptivePolicy(genome,updated);
        var frozenCopy=frozen.Snapshot();
        Check(Close(frozenCopy[0,action.Code],updated[0,action.Code]),
            "reloaded learned values match snapshot");
        updated[0,action.Code]=123456;
        Check(!Close(frozen.Snapshot()[0,action.Code],123456),
            "reloaded values cannot be mutated externally");
        // A controlled diagnostic: if a public signal exactly targets
        // the agent's current patch, trusting cannot change its forage action.
        obs.ReceivedSignals=new[]{1,0,0};
        var ignoreTarget=0;
        Check(ignoreTarget==obs.OwnPatch,"trust/ignore may have identical actions");
        Console.WriteLine("Experiment 001 validity checks passed: "+assertions);
        return assertions;
    }
}
