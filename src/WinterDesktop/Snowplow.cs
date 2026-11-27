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
        internal RoadEvent Kind;
        private RoadEvent next;
        private bool escaped;
        private double waiting, sampleDelay;
        internal Snowplow(Surface ground, ScreenArea screen) { Ground=ground; Screen=screen; }

        internal void Reset()
        {
            Active=false; waiting=sampleDelay=0; Travel=Load=StuckTime=0;
            escaped=false; next=RoadEvent.Plow;
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
                Active=true; escaped=false; Travel=Load=StuckTime=0; waiting=0;
                X=Ground.Bounds.Left-64;
            }
            if(Kind==RoadEvent.Car && !escaped) {
                float wheel=X+18;
                if(Stuck || (wheel>=Ground.Bounds.Left && wheel<Ground.Bounds.Right && Ground.HeightAt(wheel)>=3)) {
                    StuckTime+=dt;
                    if(StuckTime<3)return;
                    escaped=true; StuckTime=0;
                }
            }
            float previous=X;
            float speed=Kind==RoadEvent.Convoy ? 95 : Kind==RoadEvent.Car ? 175 : 145;
            Travel+=speed*dt; X=Ground.Bounds.Left-64+Travel;
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

