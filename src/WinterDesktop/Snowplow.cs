using System;
using System.Collections.Generic;
namespace WinterDesktop
{
    internal enum RoadEvent { Plow, Convoy, Car }

    internal sealed class Snowplow
    {
        internal readonly Surface Ground;
        internal readonly ScreenArea Screen;
        internal bool Active;
        internal float X, Travel, Load, StuckTime;
        internal bool Stuck { get { return StuckTime > 0; } }
        internal bool Wheelspin { get { return Kind==RoadEvent.Car && Stuck && Rescue==RescuePhase.None; } }
        internal RoadEvent Kind;
        private RoadEvent next;
        internal enum RescuePhase { None, Exit, Call, Approach, Hitch, Tow }
        internal RescuePhase Rescue;
        internal float TractorX, TractorTravel, Lift, Pitch;
        private float tractionUntil; internal float hitchTime;

        private void Start(RoadEvent kind)
        {
            Kind=kind; Active=true; Travel=Load=StuckTime=0; waiting=0;
            Rescue=RescuePhase.None; Lift=Pitch=TractorTravel=0;
            X=Ground.Bounds.Left-64; tractionUntil=X;
        }

        internal void Demonstrate()
        {
            if (Active) return;
            if (Ground.Accumulates) {
                // A small real drift makes the interaction visible on an empty desktop.
                for(int i=0;i<Ground.Snow.Length;i++){
                    float t=i/(float)(Ground.Snow.Length-1);
                    float mound=Math.Max(0,1-Math.Abs(t-0.48f)/0.10f);
                    Ground.Snow[i]=Math.Max(Ground.Snow[i],16*mound*mound);
                }
            }
            Start(RoadEvent.Car);
        }

        private float Depth(float x)
        {
            return x<Ground.Bounds.Left || x>Ground.Bounds.Right ? 0 : Ground.HeightAt(x);
        }

        private void Compact(float from,float to,float amount)
        {
            float scale=(Ground.Snow.Length-1)/(float)Math.Max(1,Ground.Bounds.Width);
            int first=Math.Max(0,(int)Math.Ceiling((from-Ground.Bounds.Left)*scale));
            int last=Math.Min(Ground.Snow.Length-1,(int)Math.Floor((to-Ground.Bounds.Left)*scale));
            for(int i=first;i<=last;i++) Ground.Snow[i]=Math.Max(0,Ground.Snow[i]-amount);
        }

        private float CarSpeed(float dt)
        {
            float rear=Depth(X-17), front=Depth(X+18);
            float ahead=Depth(X+23);
            float resistance=Math.Max(front,ahead);
            float frontSupport=Math.Min(12,front*0.55f), rearSupport=Math.Min(12,rear*0.55f);
            Lift+=(0.5f*(frontSupport+rearSupport)-Lift)*Math.Min(1,dt*14);
            Pitch+=((float)(Math.Atan2(rearSupport-frontSupport,35)*180/Math.PI)-Pitch)*Math.Min(1,dt*14);
            if(Rescue==RescuePhase.None && resistance>=10){
                Rescue=RescuePhase.Exit; hitchTime=0;
                TractorX=Ground.Bounds.Left-64;
            }
            if(Rescue==RescuePhase.Exit || Rescue==RescuePhase.Call){
                StuckTime+=dt;
                hitchTime+=dt;
                if(hitchTime >= (Rescue==RescuePhase.Exit ? 1f : 2.5f)){
                    Rescue=Rescue==RescuePhase.Exit ? RescuePhase.Call : RescuePhase.Approach;
                    hitchTime=0;
                }
                return 0;
            }
            if(Rescue==RescuePhase.Approach){
                float gap=X+81-TractorX;
                float step=Math.Min(Math.Max(0,gap),165*dt);
                TractorX+=step; TractorTravel+=step;
                if(gap<=step){Rescue=RescuePhase.Hitch;hitchTime=0;}
                StuckTime+=dt;return 0;
            }
            if(Rescue==RescuePhase.Hitch){
                StuckTime+=dt; hitchTime+=dt; TractorX+=8*dt; TractorTravel+=8*dt;
                if(hitchTime>=1){Rescue=RescuePhase.Tow;StuckTime=0;}
                return 0;
            }
            if(Rescue==RescuePhase.Tow){
                TractorX+=78*dt;TractorTravel+=78*dt;return 78;
            }
            if(Stuck || (resistance>=4.5f && X>=tractionUntil)){
                StuckTime+=dt;
                Compact(X+13,X+24,dt*2);
                if(StuckTime<0.8f)return 0;
                StuckTime=0;tractionUntil=X+32;
            }
            return Math.Max(45,175-12*resistance);
        }
        private double waiting, sampleDelay;
        internal Snowplow(Surface ground, ScreenArea screen) { Ground=ground; Screen=screen; }

        internal void Reset()
        {
            Active=false; waiting=sampleDelay=0; Travel=Load=StuckTime=0;
            Rescue=RescuePhase.None; next=RoadEvent.Plow;
        }

        internal void Step(float dt,List<PlowSpray> spray,Random random)
        {
            if (!Active) {
                waiting+=dt;
                sampleDelay-=dt;
                if(sampleDelay>0)return;
                sampleDelay=0.5;
                float total=0;
                foreach(float depth in Ground.Snow) total+=depth;
                if (total<0.1f) { waiting=0; return; }
                if (waiting<120 || total<Ground.Snow.Length*0.15f) return;
                Kind=next;
                next=Kind==RoadEvent.Car ? RoadEvent.Plow : (RoadEvent)((int)Kind+1);
                Start(Kind);
            }
            float previous=X;
            float previousSlip=StuckTime;
            float speed=Kind==RoadEvent.Car ? CarSpeed(dt) : Kind==RoadEvent.Convoy ? 95 : 145;
            Travel+=speed*dt; X=Ground.Bounds.Left-64+Travel;
            if(Wheelspin){
                int count=(int)(StuckTime*32)-(int)(previousSlip*32);
                double pitch=Pitch*Math.PI/180;
                float contactX=X-17*(float)Math.Cos(pitch);
                float contactY=Ground.Bounds.Top-1-Lift-17*(float)Math.Sin(pitch);
                for(int i=0;i<count;i++){
                    spray.Add(new PlowSpray { Screen=Screen, X=contactX-4, Y=contactY-2,
                        VX=-55-(float)random.NextDouble()*65, VY=-38-(float)random.NextDouble()*33,
                        Radius=0.8f+(float)random.NextDouble()*1.2f, Life=0.32f+(float)random.NextDouble()*0.16f });
                }
            }
            if(Kind==RoadEvent.Car && speed>0) {
                float pressure=dt*speed*0.22f;
                Compact(previous-20,X-14,pressure);
                Compact(previous+15,X+21,pressure);
            }
            if(Kind!=RoadEvent.Car) {
                float collected=Sweep(previous+44,X+52);
                Load+=collected;
                if(collected>0.01f) {
                    int count=Math.Min(3,1+(int)(collected*0.08f));
                    for(int i=0;i<count;i++) {
                        bool chute=Kind==RoadEvent.Convoy;
                        spray.Add(new PlowSpray {Screen=Screen,X=X+(chute?-48:52),Y=Ground.Bounds.Top-(chute?52:9),
                            VX=chute?-50:35+(float)random.NextDouble()*55,
                            VY=chute?-20:-45-(float)random.NextDouble()*85,
                            Radius=1+(float)random.NextDouble()*1.8f,Life=chute?0.48f:0.45f+(float)random.NextDouble()*0.6f});
                    }
                }
            }
            if(X>Ground.Bounds.Right+(Kind==RoadEvent.Convoy ? 160 : 64)) {Active=false;waiting=0;sampleDelay=0;}
        }
        private float Sweep(float left,float right)
        {
            float scale=(Ground.Snow.Length-1)/(float)Math.Max(1,Ground.Bounds.Width);
            int start=Math.Max(0,(int)Math.Floor((left-Ground.Bounds.Left)*scale));
            int end=Math.Min(Ground.Snow.Length-1,(int)Math.Ceiling((right-Ground.Bounds.Left)*scale));
            float removed=0;
            for(int i=start;i<=end;i++){removed+=Ground.Snow[i];Ground.Snow[i]=0;}
            return removed;
        }
    }
}



