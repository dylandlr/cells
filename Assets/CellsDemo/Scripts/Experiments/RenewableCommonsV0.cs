using System;

namespace Cells.Experiments
{
    /// <summary>
    /// Minimal renewable commons, independent of Unity rendering.
    /// Research scenario v0: four bounded-rational organisms, three patches,
    /// resource stocks, complementary restoration, scarce energy, and costly
    /// one-step messages. No genetically evolving agents yet.
    /// </summary>
    public sealed class RenewableCommonsV0
    {
        public const int Players=4,Patches=3;
        public const int Harvest=0,Restore=3,Signal=6,Rest=9;
        public const int ActionCount=10;
        public const double InitialEnergy=5;
        public const double HarvestCost=.2,RestoreCost=.6,SignalCost=.3,RestGain=.05;
        public const int RepairThreshold=2;
        public struct View
        {
            public int Step,Seat,Location,LocalStock,LocalHazard;
            public double Energy;
            public int[] Heard;
            public bool Alive;
        }
        public struct Act
        {
            public int Code;
            public Act(int code){Code=code;}
        }
        public struct StepLog
        {
            public int Step,SuccessfulRepairs,HarvestedUnits,Deaths;
            public int[] StocksBefore,StocksAfter,Actions;
            public double[] Rewards;
        }
        private readonly int[] stocks=new int[Patches];
        private int substrateReserve=120;
        public int SubstrateReserve=>substrateReserve;
        public int TotalExternalNutrients()
        {
            int total=substrateReserve;
            for(int p=0;p<Patches;p++)total+=stocks[p];
            return total;
        }
        private readonly int[] hazard=new int[Patches];
        private readonly int[] locations=new int[Players];
        private readonly double[] energy=new double[Players];
        private readonly double[] returns=new double[Players];
        private readonly bool[] alive=new bool[Players];
        private readonly int[] messages=new int[Patches];
        private readonly Random environment;
        private readonly bool delivery;
        private readonly int deliveryStride;
        private readonly double receptionProbability;
        private readonly Random receptionRandom;
        private int broadcastEvents;
        private bool latestBroadcastVisible;
        private int step;
        public int Step=>step;
        public RenewableCommonsV0(int seed,bool deliverMessages=true,int messageDeliveryStride=1,double messageReceptionProbability=1.0)
        {
            if(messageDeliveryStride<1)throw new ArgumentOutOfRangeException("messageDeliveryStride");
            if(double.IsNaN(messageReceptionProbability)||messageReceptionProbability<0||messageReceptionProbability>1)throw new ArgumentOutOfRangeException("messageReceptionProbability");
            deliveryStride=messageDeliveryStride;
            receptionProbability=messageReceptionProbability;
            receptionRandom=new Random(unchecked(seed*48611+19237));
            environment=new Random(seed);
            delivery=deliverMessages;
            for(int p=0;p<Patches;p++)
            {stocks[p]=3+environment.Next(3);hazard[p]=environment.Next(3);}
            for(int i=0;i<Players;i++)
            {locations[i]=i%Patches;energy[i]=InitialEnergy;alive[i]=true;}
        }
        public int[] Stocks()=> (int[])stocks.Clone();
        public double[] Returns()=> (double[])returns.Clone();
        public View Observe(int seat)
        {
            if(seat<0||seat>=Players)throw new ArgumentOutOfRangeException("seat");
            int where=locations[seat];
            return new View{
                Step=step,Seat=seat,Location=where,
                LocalStock=stocks[where],LocalHazard=hazard[where],
                Energy=energy[seat],Alive=alive[seat],
                Heard=delivery && latestBroadcastVisible?(int[])messages.Clone():new int[Patches]
            };
        }
        public StepLog Advance(Act[] acts)
        {
            if(acts==null||acts.Length!=Players)throw new ArgumentException("Four actions required");
            int[] before=Stocks(),repairers=new int[Patches],
                nextMessages=new int[Patches],
                requested=new int[Players];
            for(int i=0;i<Players;i++)
            {
                int code=acts[i].Code;
                if(code<0||code>=ActionCount)throw new ArgumentOutOfRangeException("acts");
                requested[i]=code;
                if(!alive[i])continue;
                if(code>=3&&code<6)repairers[code-3]++;
                if(code>=6&&code<9)nextMessages[code-6]++;
            }
            var pay=new double[Players];
            int harvested=0;
            // One unit of stock per forager at a patch, allocated in seat
            // order; no magical duplicated resources.
            for(int i=0;i<Players;i++)
            {
                if(!alive[i])continue;
                int a=requested[i];
                double gain;
                if(a<3)
                {
                    double move=locations[i]==a?0:.2;
                    locations[i]=a;
                    if(stocks[a]>0)
                    {
                        stocks[a]--;harvested++;
                        gain=2.0-HarvestCost-move-.1*hazard[a];
                    }
                    else gain=-HarvestCost-move-.1*hazard[a];
                }
                else if(a<6)
                {
                    locations[i]=a-3;
                    gain=-RestoreCost;
                }
                else if(a<9)gain=-SignalCost;
                else gain=RestGain;
                pay[i]=gain;returns[i]+=gain;energy[i]+=gain;
            }
            int successful=0;
            // Repair is a true coordination threshold, with a delayed,
            // common-pool benefit rather than immediate private reward.
            for(int p=0;p<Patches;p++)
                if(repairers[p]>=RepairThreshold)
                {
                    int transfer=Math.Min(4,Math.Min(substrateReserve,8-stocks[p]));
                    stocks[p]+=transfer;
                    substrateReserve-=transfer;
                    if(transfer>0)successful++;
                }
            int deaths=0;
            for(int i=0;i<Players;i++)
                if(alive[i]&&energy[i]<=0){alive[i]=false;deaths++;}
            int broadcastCount=0;
            for(int p=0;p<Patches;p++)broadcastCount+=nextMessages[p];
            if(broadcastCount>0)
            {
                broadcastEvents++;
                latestBroadcastVisible=broadcastEvents%deliveryStride==0 && receptionRandom.NextDouble()<receptionProbability;
            }
            else latestBroadcastVisible=false;
            Array.Copy(nextMessages,messages,Patches);
            // Slow stochastic replenishment, independent of actions.
            for(int p=0;p<Patches;p++)
                if(environment.NextDouble()<.025 && stocks[p]<8 && substrateReserve>0)
                {stocks[p]++;substrateReserve--;}
            var outcome=new StepLog{Step=step,SuccessfulRepairs=successful,
                HarvestedUnits=harvested,Deaths=deaths,
                StocksBefore=before,StocksAfter=Stocks(),Actions=requested,
                Rewards=pay};
            step++;
            return outcome;
        }
    }
}
