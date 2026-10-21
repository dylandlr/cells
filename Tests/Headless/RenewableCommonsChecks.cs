using System;
using Cells.Experiments;

internal static class RenewableCommonsChecks
{
    private static int count;
    private static void Check(bool value,string name)
    {
        if(!value)throw new Exception("RENEWABLE COMMONS FAILED: "+name);
        count++;
    }
    public static int Run()
    {
        count=0;
        var world=new RenewableCommonsV0(44);
        var copy=new RenewableCommonsV0(44);
        var stocks=world.Stocks();
        Check(stocks.Length==3&&stocks[0]>=3&&stocks[0]<=5,
            "bounded initial stock");
        stocks[0]=-99;
        Check(world.Stocks()[0]>=3,"resource stock snapshots defensive");
        var allHarvest=new []{
            new RenewableCommonsV0.Act(0),new RenewableCommonsV0.Act(0),
            new RenewableCommonsV0.Act(0),new RenewableCommonsV0.Act(0)};
        var initial=world.Stocks()[0];
        int initialTotal=world.TotalExternalNutrients();
        var first=world.Advance(allHarvest);
        var replay=copy.Advance(allHarvest);
        Check(world.TotalExternalNutrients()==initialTotal-first.HarvestedUnits,
            "harvested external nutrients accounted for");
        Check(first.HarvestedUnits==Math.Min(initial,4),"resource harvesting cannot duplicate stock");
        Check(first.HarvestedUnits==replay.HarvestedUnits &&
              first.StocksAfter[0]==replay.StocksAfter[0],
              "matched environmental seed reproducible");
        Check(first.StocksBefore[0]==initial,"trace before-stock correct");
        Check(first.Rewards[0]>0,"early harvester rewarded");
        var cooperators=new[]{
            new RenewableCommonsV0.Act(3),new RenewableCommonsV0.Act(3),
            new RenewableCommonsV0.Act(9),new RenewableCommonsV0.Act(9)};
        int depleted=world.Stocks()[0];
        int beforeRepairTotal=world.TotalExternalNutrients();
        var repaired=world.Advance(cooperators);
        Check(world.TotalExternalNutrients()==beforeRepairTotal,
            "restoration and passive replenishment conserve external nutrients");
        Check(repaired.SuccessfulRepairs==1,"two restorers succeed");
        Check(repaired.StocksAfter[0]>=depleted+4 || repaired.StocksAfter[0]==8,
            "restoration adds renewable stock");
        Check(world.SubstrateReserve<120,"restoration draws finite reserve");
        Check(repaired.Rewards[0]<0 && repaired.Rewards[1]<0,
            "restoration costs immediate energy");
        var solitary=new RenewableCommonsV0(44);
        var lone=solitary.Advance(new[]{
            new RenewableCommonsV0.Act(3),new RenewableCommonsV0.Act(9),
            new RenewableCommonsV0.Act(9),new RenewableCommonsV0.Act(9)});
        Check(lone.SuccessfulRepairs==0,"one restorer insufficient");
        var on=new RenewableCommonsV0(88,true);
        var off=new RenewableCommonsV0(88,false);
        var send=new[]{
            new RenewableCommonsV0.Act(7),new RenewableCommonsV0.Act(9),
            new RenewableCommonsV0.Act(9),new RenewableCommonsV0.Act(9)};
        var r1=on.Advance(send);var r2=off.Advance(send);
        var sparse=new RenewableCommonsV0(88,true,2);
        sparse.Advance(send);
        Check(sparse.Observe(2).Heard[1]==0,"stride two suppresses step-one signal");
        Check(sparse.SubstrateReserve==on.SubstrateReserve,
            "delivery masking does not affect sender's fixed-action resource accounting");
        Check(on.Observe(2).Heard[1]==1 && off.Observe(2).Heard[1]==0,
            "one-step message-delivery ablation");
        Check(Math.Abs(r1.Rewards[0]-r2.Rewards[0])<1e-9,
            "sender cost preserved by ablation");
        Check(on.Observe(0).LocalStock>=0,"agent can read local stock");
        Check(on.Observe(0).Heard.Length==3,"message channel count");
        // A strategic opportunity test rather than a performance claim:
        // first harvested resources deplete; coordinating restores stock.
        Check(repaired.StocksAfter[0]>first.StocksAfter[0],
            "joint repair produces a resource recovery opportunity");
        var depletion=new RenewableCommonsV0(777);
        int lastTotal=depletion.TotalExternalNutrients();
        for(int turn=0;turn<150;turn++)
        {
            var beforeTotal=depletion.TotalExternalNutrients();
            var restoration=depletion.Advance(new[]{
                new RenewableCommonsV0.Act(3),new RenewableCommonsV0.Act(3),
                new RenewableCommonsV0.Act(4),new RenewableCommonsV0.Act(4)});
            // This repeated restoration test intentionally burns energy; once
            // participants die, no restoration can occur.
            Check(depletion.TotalExternalNutrients()==beforeTotal,
                "transfer never manufactures external stock "+turn);
            Check(depletion.SubstrateReserve>=0,
                "substrate reserve never negative "+turn);
            lastTotal=depletion.TotalExternalNutrients();
        }
        Check(lastTotal>0,"external nutrients stay accounted for");
        // This particular death-bounded path may not exhaust the reserve;
        // a reserve reaching zero is not required to pass.
        Console.WriteLine("Renewable commons validity checks passed: "+count);
        return count;
    }
}
