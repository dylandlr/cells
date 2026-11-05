using System;
using Cells.Automata;
internal static class CellMachineChecks
{
    public static void Run()
    {
        var cell = new CellMachineV01(0.5, 0.5);
        double bath = 30;
        double consumed = 0;
        for(int i=0; i<100; i++)
        {
            var frame = cell.Step(ref bath, 0.05);
            consumed += frame.Consumed;
            Assert(Math.Abs(bath + cell.Nutrient + consumed - 30)<1e-7, "Resource balance");
            Assert(bath>=0 && cell.Nutrient>=0 && cell.Energy>=0, "Nonnegative resources");
            Assert(cell.Integrity>=0 && cell.Integrity<=1, "Membrane bounded");
        }
        var damaged = new CellMachineV01(0,0,2,0.1);
        var intact = new CellMachineV01(0,0,2,1);
        double a=10,b=10;
        Assert(intact.Step(ref b,0).Transported>damaged.Step(ref a,0).Transported,"Membrane gating");
        Assert(CellMachineV01.Trace(3).Split('\n').Length==5,"Trace format");
    }
    private static void Assert(bool ok,string message)
    {
        if(!ok) throw new Exception("Cell machine: "+message);
    }
}