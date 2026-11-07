using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Automata
{
    // Minimal research prototype. Explicit inventory accounting; not a biochemical calibration.
    public sealed class CellMachineV01
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Nutrient { get; private set; }
        public double Energy { get; private set; }
        public double Integrity { get; private set; }
        public double Sensor { get; private set; }
        public double TransportCapacity { get; set; } = 0.4;
        public double MetabolicCapacity { get; set; } = 0.3;
        public double RepairCapacity { get; set; } = 0.12;
        public double UptakeEnergyCost { get; set; } = 0.04;
        public double MetabolicYield { get; set; } = 0.75;
        public double MetabolicHeatLoss { get; private set; }
        public int Tick { get; private set; }
        public CellMachineV01(double x,double y,double energy=2,double integrity=0.8)
        {
            if(!Valid(x)||!Valid(y)||!Valid(energy)||!Valid(integrity)||energy<0||integrity<0||integrity>1)
                throw new ArgumentException("Invalid starting machine state");
            X=x;Y=y;Energy=energy;Integrity=integrity;
        }
        private static bool Valid(double d)=>!double.IsNaN(d)&&!double.IsInfinity(d);
        public readonly struct Frame
        {
            public readonly int Tick;
            public readonly double X,Y,Nutrient,Energy,Integrity,Sensor,Bath,Consumed,Transported,Repaired,Waste;
            public Frame(CellMachineV01 c,double bath,double consumed,double transported,double repaired,double waste)
            {Tick=c.Tick;X=c.X;Y=c.Y;Nutrient=c.Nutrient;Energy=c.Energy;Integrity=c.Integrity;Sensor=c.Sensor;Bath=bath;Consumed=consumed;Transported=transported;Repaired=repaired;Waste=waste;}
        }
        // Bath is in abstract resource units. Uptake is physically gated by membrane
        // integrity. Catabolism transfers a fraction of substrate to energy, with
        // explicit remainder placed into waste; numerical conservation is auditable.
        public Frame Step(ref double bath,double repairDemand)
        {
            if(!Valid(bath)||bath<0||!Valid(repairDemand)||repairDemand<0)
                throw new ArgumentException("Invalid bath or demand");
            if(TransportCapacity<0||MetabolicCapacity<0||RepairCapacity<0||UptakeEnergyCost<0||
               MetabolicYield<0||MetabolicYield>1||!Valid(TransportCapacity)||!Valid(MetabolicCapacity)||
               !Valid(RepairCapacity)||!Valid(UptakeEnergyCost)||!Valid(MetabolicYield))
                throw new InvalidOperationException("Invalid machine configuration");
            Sensor=bath/(1+bath); // saturation with finite receptor dynamic range
            double transport=Math.Min(Math.Min(bath,TransportCapacity*Integrity*Sensor),
                UptakeEnergyCost==0?bath:Energy/UptakeEnergyCost);
            bath-=transport;Nutrient+=transport;Energy-=transport*UptakeEnergyCost;
            double consumed=Math.Min(Nutrient,MetabolicCapacity);
            Nutrient-=consumed;Energy+=consumed*MetabolicYield;
            double waste=consumed*(1-MetabolicYield);MetabolicHeatLoss+=waste;
            double repaired=Math.Min(Math.Min(repairDemand,RepairCapacity),Math.Min(1-Integrity,Energy));
            Integrity+=repaired;Energy-=repaired;
            Tick++;
            return new Frame(this,bath,consumed,transport,repaired,waste);
        }
        public static string Trace(int steps=100)
        {
            if(steps<1||steps>10000)throw new ArgumentOutOfRangeException(nameof(steps));
            var c=new CellMachineV01(0.5,0.5);
            double bath=30;
            var sb=new StringBuilder("tick,x,y,bath,nutrient,energy,membrane_integrity,sensor,transported,metabolized,repaired,waste\n");
            for(int i=0;i<steps;i++)
            {
                var f=c.Step(ref bath,0.05);
                string F(double d)=>d.ToString("0.000000",CultureInfo.InvariantCulture);
                sb.Append(f.Tick).Append(',').Append(F(f.X)).Append(',').Append(F(f.Y)).Append(',')
                  .Append(F(f.Bath)).Append(',').Append(F(f.Nutrient)).Append(',')
                  .Append(F(f.Energy)).Append(',').Append(F(f.Integrity)).Append(',')
                  .Append(F(f.Sensor)).Append(',').Append(F(f.Transported)).Append(',')
                  .Append(F(f.Consumed)).Append(',').Append(F(f.Repaired)).Append(',')
                  .Append(F(f.Waste)).Append('\n');
            }
            return sb.ToString();
        }
    }
}
