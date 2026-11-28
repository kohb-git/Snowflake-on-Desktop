using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
namespace WinterDesktop {
 internal static class RoadTests {
 [STAThread] static int Main(){
 var w=new World(42); var bounds=new Rectangle(0,0,1920,1080);
 w.SetScreens(new List<ScreenArea>{new ScreenArea("one",bounds,bounds)});
 var infos=new List<WindowInfo>();
 for(int i=0;i<18;i++)infos.Add(new WindowInfo(i.ToString(),new Rectangle((i*101)%1500,90+i*37,400,220),true,false));
 w.SetWindows(infos); w.SetIntensity(180);
 for(int i=0;i<100;i++)w.Step(1f/120);
 var times=new List<double>();for(int batch=0;batch<35;batch++){var watch=Stopwatch.StartNew();for(int i=0;i<60;i++)w.Step(1f/120);times.Add(watch.Elapsed.TotalMilliseconds/60);}
 times.Sort(); Console.WriteLine("simulation ms per step (median batch): "+times[17].ToString("F3"));
 var flags=BindingFlags.NonPublic|BindingFlags.Instance;
 using(var snow=new SnowWindow()){
 snow.Show();Application.DoEvents();var tray=(NotifyIcon)typeof(SnowWindow).GetField("tray",flags).GetValue(snow);
 var sw=Stopwatch.StartNew();
 foreach(ToolStripItem item in tray.ContextMenuStrip.Items)if(item.Text.StartsWith("Настройки"))item.PerformClick();
 while(sw.ElapsedMilliseconds<2000){Application.DoEvents();var f=(Form)typeof(SnowWindow).GetField("options",flags).GetValue(snow);if(f!=null && f.Visible){Console.WriteLine("Settings opened, ms: "+sw.ElapsedMilliseconds);f.Close();break;}}
 if(sw.ElapsedMilliseconds>=2000)throw new Exception("Settings timeout");snow.Close();}

 var vehicle=w.Plows[0];vehicle.Reset();var random=new Random(42);
 for(int round=0;round<3;round++){
 bool seen=false,stuck=false,delivered=false,rendered=false;
 using(var image=new Bitmap(340,100))using(var g=Graphics.FromImage(image)){
 for(int tick=0;tick<30000;tick++){
 if(!seen && !vehicle.Active)for(int k=0;k<vehicle.Ground.Snow.Length;k++)vehicle.Ground.Snow[k]=5f;
 vehicle.Step(1f/120,w.Spray,random);
 if(vehicle.Active){if((int)vehicle.Kind!=round)throw new Exception("sequence");seen=true;stuck|=vehicle.Stuck;delivered|=vehicle.Load>0;
 if(!rendered){g.TranslateTransform(170,80);RoadPainter.Draw(g,vehicle);int pixels=0;for(int x=0;x<340;x++)for(int y=0;y<100;y++)if(image.GetPixel(x,y).A>0)pixels++;if(pixels<100)throw new Exception("sprite");rendered=true;}}
 if(seen && !vehicle.Active)break;
 }}
 if(!seen || vehicle.Active || round==1 && !delivered || round==2 && !stuck)throw new Exception("event "+round);
 Console.WriteLine(vehicle.Kind+" passed");}
 w.Clear();if(vehicle.Active || vehicle.Load!=0 || vehicle.Stuck)throw new Exception("reset");

 foreach(float depth in new[]{0f,2f,6f,14f}){
 var ground=new Surface("test",new Rectangle(-400,600,800,2),true);
 var car=new Snowplow(ground,new ScreenArea("test",new Rectangle(-400,0,800,600),new Rectangle(-400,0,800,600)));
 car.Demonstrate();
 for(int j=0;j<ground.Snow.Length;j++)ground.Snow[j]=(j>35 && j<95)?depth:0;
 bool slip=false,tow=false,hitch=false;float tilt=0;
 for(int tick=0;tick<12000 && car.Active;tick++){
 car.Step(1f/120,w.Spray,random);slip|=car.Stuck;tow|=car.Rescue==Snowplow.RescuePhase.Tow;
 hitch|=car.Rescue==Snowplow.RescuePhase.Hitch;tilt=Math.Max(tilt,Math.Abs(car.Pitch));}
 if(car.Active || slip!=(depth>=4.5f) || tow!=(depth>=10) || hitch!=tow ||
 (depth>0 && (ground.Snow[60]>=depth || tilt==0)))throw new Exception("Collision failed: "+depth);
 Console.WriteLine("Depth "+depth+": slip="+slip+", tow="+tow+", track="+ground.Snow[60]);}
 return 0;
 }}}

