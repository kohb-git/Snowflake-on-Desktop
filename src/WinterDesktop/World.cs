using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinterDesktop
{
	public sealed partial class World
	{
		

		internal readonly List<Surface> Surfaces = new List<Surface>();

		internal readonly List<Flake> Flakes = new List<Flake>();

		internal readonly List<Ball> Balls = new List<Ball>();

		internal List<ScreenArea> Screens = new List<ScreenArea>();

		internal readonly List<Rectangle> Taskbars = new List<Rectangle>();

		internal readonly List<Rectangle> WindowCovers = new List<Rectangle>();

		internal readonly List<Rectangle> FullScreenCovers = new List<Rectangle>();

		internal readonly List<LooseSnow> Loose = new List<LooseSnow>();

		internal readonly Dictionary<string, Rectangle> WindowRects = new Dictionary<string, Rectangle>();

		internal readonly List<Snowplow> Plows = new List<Snowplow>();

		internal readonly List<PlowSpray> Spray = new List<PlowSpray>();

		internal Ball Held;

		private readonly Random random;

		private double time;

		internal PointF Pointer;

		internal bool PointerPresent;

		private PointF lastSnowPointer;

		private bool trackedSnowPointer;

		internal readonly List<Powder> Dust = new List<Powder>();

		internal bool AccumulateGround = true;

		internal bool AccumulateWindows = true;

		internal bool MeltSnow;

		internal bool NaturalSnow;

		internal int Intensity = 100;

		internal int GarlandTheme;

		internal float LampScale = 1f;

		internal float LightLevel = 1f;

		internal float TwinkleSpeed = 1f;

		internal float SwayStrength = 1f;

		internal bool GarlandEnabled;
		internal Preferences Display = new Preferences();

		internal bool MotionEffectsEnabled = true;

		internal int SnowfallLevel = 1;

		internal int LayoutVersion;

		public List<WindowInfo> windowSnapshot;

		internal double AnimationTime => time;

		internal void SetAccumulation(bool ground, bool windows)
		{
			AccumulateGround = ground;
			AccumulateWindows = windows;
			foreach (Surface surface in Surfaces)
			{
				surface.Accumulates = (surface.Ground ? ground : windows);
				if (!surface.Accumulates)
				{
					Array.Clear(surface.Snow, 0, surface.Snow.Length);
				}
			}
		}

		internal void SetIntensity(int intensity)
		{
			Intensity = Math.Max(0, Math.Min(200, intensity));
			RefreshFlakes();
		}

		private void EvolveSnow(float dt)
		{
			if (!MeltSnow && !NaturalSnow) return;
			float settlingRate = Math.Min(0.2f, dt * 0.6f);
			foreach (Surface surface in Surfaces)
			{
				if (!surface.Accumulates)
				{
					continue;
				}
				for (int i = 0; i < surface.Snow.Length; i++)
				{
					if (MeltSnow)
					{
						surface.Snow[i] = Math.Max(0f, surface.Snow[i] - 0.035f * dt);
					}
					if (NaturalSnow && i + 1 < surface.Snow.Length)
					{
						float num = surface.Snow[i] - surface.Snow[i + 1];
						float num2 = Math.Max(0f, Math.Abs(num) - 3f) * settlingRate;
						if (num > 0f)
						{
							surface.Snow[i] -= num2;
							surface.Snow[i + 1] += num2;
						}
						else
						{
							surface.Snow[i] += num2;
							surface.Snow[i + 1] -= num2;
						}
					}
				}
			}
		}

		private void Burst(LooseSnow p)
		{
			if (p.Chunk && Dust.Count <= 600)
			{
				for (int i = 0; i < 8; i++)
				{
					Dust.Add(new Powder
					{
						X = p.X,
						Y = p.Y,
						VX = (float)(random.NextDouble() - 0.5) * 100f,
						VY = -15f - (float)random.NextDouble() * 60f,
						Radius = 0.7f + (float)random.NextDouble() * 1.4f,
						Life = 0.35f + (float)random.NextDouble() * 0.4f,
						FrontWindows = p.FrontWindows
					});
				}
			}
		}

		private void StepDust(float dt)
		{
			for (int num = Dust.Count - 1; num >= 0; num--)
			{
				Powder powder = Dust[num];
				powder.Age += dt;
				powder.VY += 150f * dt;
				powder.X += powder.VX * dt;
				powder.Y += powder.VY * dt;
				if (powder.Age >= powder.Life)
				{
					Dust.RemoveAt(num);
				}
			}
		}

		private int FlakeCount(ScreenArea screen)
		{
			double num = (double)Intensity / 100.0;
			return (int)((double)Math.Max(60L, Math.Min(650L, (long)screen.Bounds.Width * (long)screen.Bounds.Height / 10500)) * num);
		}

		internal void SetSnowfall(int level)
		{
			SnowfallLevel = Math.Max(0, Math.Min(2, level));
			SetIntensity((SnowfallLevel == 0) ? 50 : ((SnowfallLevel == 2) ? 170 : 100));
		}

		private void RefreshFlakes()
		{
			foreach (ScreenArea screen in Screens)
			{
				int num = FlakeCount(screen);
				List<Flake> flakes = Flakes;
				Predicate<Flake> match = (Flake f) => f.Screen == screen;
				int i;
				for (i = flakes.FindAll(match).Count; i < num; i++)
				{
					Flakes.Add(NewFlake(screen, scatter: true));
				}
				int num2 = Flakes.Count - 1;
				while (num2 >= 0 && i > num)
				{
					if (Flakes[num2].Screen == screen)
					{
						Flakes.RemoveAt(num2);
						i--;
					}
					num2--;
				}
			}
		}

		public World(int seed)
		{
			random = new Random(seed);
		}

		private void ResizeSnow(Surface source, Rectangle target)
		{
			Rectangle bounds = source.Bounds;
			float[] snow = source.Snow;
			int num = Math.Max(2, (target.Width + 5) / 6 + 1);
			float[] array = new float[num];
			for (int i = 0; i < snow.Length; i++)
			{
				float num2 = (float)bounds.Left + (float)(i * bounds.Width) / (float)snow.Length;
				float num3 = (float)bounds.Left + (float)((i + 1) * bounds.Width) / (float)snow.Length;
				float num4 = Math.Max(num2, target.Left);
				float num5 = Math.Min(num3, target.Right);
				float num6 = Math.Max(0f, num5 - num4) / (num3 - num2);
				float num7 = snow[i] * (1f - num6);
				if (num6 > 0f)
				{
					int num8 = Math.Max(0, (int)((num4 - (float)target.Left) * (float)num / (float)target.Width));
					int num9 = Math.Min(num - 1, (int)((num5 - (float)target.Left) * (float)num / (float)target.Width));
					for (int j = num8; j <= num9; j++)
					{
						float val = (float)target.Left + (float)(j * target.Width) / (float)num;
						float val2 = (float)target.Left + (float)((j + 1) * target.Width) / (float)num;
						array[j] += snow[i] * Math.Max(0f, Math.Min(num5, val2) - Math.Max(num4, val)) / (num3 - num2);
					}
				}
				if (num7 > 1E-05f)
				{
					float num10 = ((num2 < (float)target.Left) ? num2 : Math.Max(num2, target.Right));
					float num11 = ((num2 < (float)target.Left) ? Math.Min(num3, target.Left) : num3);
					float x = num10 + (float)random.NextDouble() * Math.Max(0f, num11 - num10);
					Loose.Add(new LooseSnow
					{
						X = x,
						Y = (float)(bounds.Top + 2) + (float)random.NextDouble() * 10f,
						Mass = num7,
						Radius = Math.Min(4f, 0.8f + (float)Math.Sqrt(num7) * 0.55f),
						VX = (float)(random.NextDouble() - 0.5) * 45f,
						VY = 12f + (float)random.NextDouble() * 75f,
						FallSpeed = 85f + (float)random.NextDouble() * 100f,
						ShakenFrom = source.Id,
						FrontWindows = source.FrontWindows
					});
				}
			}
			source.Snow = array;
			source.Push = new float[num];
			source.Bounds = target;
		}

		private void Shake(Surface source, Rectangle previous)
		{
			float num = source.Bounds.X - previous.X;
			float num2 = source.Bounds.Y - previous.Y;
			float num3 = (float)Math.Sqrt(num * num + num2 * num2);
			if (num3 <= 0f)
			{
				return;
			}
			float num4 = 1f - (float)Math.Exp((0f - num3) / 180f);
			float num5 = 1f - (float)Math.Exp((0f - num3) / 45f);
			for (int i = 0; i < source.Snow.Length; i++)
			{
				if (random.NextDouble() > (double)num5)
				{
					continue;
				}
				float num6 = Math.Min(source.Snow[i], source.Snow[i] * num4 / num5 * (0.65f + (float)random.NextDouble() * 0.7f));
				if (!(num6 <= 0.001f) && Loose.Count < 5000)
				{
					float x = (float)source.Bounds.Left + ((float)i + (float)random.NextDouble()) * (float)source.Bounds.Width / (float)source.Snow.Length;
					if (source.Visible(x, source.Bounds.Top - 1) && InSnowArea(new PointF(x, source.Bounds.Top)))
					{
						source.Snow[i] -= num6;
						Loose.Add(new LooseSnow
						{
							X = x,
							Y = (float)(source.Bounds.Top + 2) + (float)random.NextDouble() * 10f,
							Mass = num6,
							Radius = Math.Min(3.6f, 0.7f + (float)Math.Sqrt(num6) * 0.55f) * (0.8f + (float)random.NextDouble() * 0.4f),
							VX = Math.Max(-100f, Math.Min(100f, (0f - num) * 2f)) + (float)(random.NextDouble() - 0.5) * 24f,
							VY = 12f + (float)random.NextDouble() * 75f,
							FallSpeed = 85f + (float)random.NextDouble() * 100f,
							ShakenFrom = source.Id,
							FrontWindows = source.FrontWindows
						});
					}
				}
			}
		}

		private void Shed(Surface source)
		{
			int num = ((!NaturalSnow) ? 1 : 3);
			for (int i = 0; i < source.Snow.Length; i += num)
			{
				float num2 = 0f;
				for (int j = i; j < Math.Min(i + num, source.Snow.Length); j++)
				{
					num2 += source.Snow[j];
				}
				if (!(num2 <= 0.01f))
				{
					float x = (float)source.Bounds.Left + ((float)i + (float)random.NextDouble() * (float)Math.Min(num, source.Snow.Length - i)) * (float)source.Bounds.Width / (float)source.Snow.Length;
					Loose.Add(new LooseSnow
					{
						X = x,
						Y = (float)source.Bounds.Top - Math.Min(10f, num2 * 0.2f),
						VX = (float)(random.NextDouble() - 0.5) * 35f,
						VY = 18f + (float)random.NextDouble() * 40f,
						Radius = Math.Min(NaturalSnow ? 8f : 4.5f, 1f + (float)Math.Sqrt(num2) * 0.65f),
						Mass = num2,
						Chunk = NaturalSnow,
						FrontWindows = source.FrontWindows
					});
				}
			}
		}

		private void StepLoose(float dt)
		{
			float num = 1f - (float)Math.Exp(-2.4 * (double)dt);
			float num2 = (float)Math.Exp(-1.4 * (double)dt);
			float num3 = MaxBottom() + 20f;
			for (int num4 = Loose.Count - 1; num4 >= 0; num4--)
			{
				LooseSnow looseSnow = Loose[num4];
				float num5 = looseSnow.Y + looseSnow.Radius;
				looseSnow.Age += dt;
				if (looseSnow.FallSpeed > 0f)
				{
					looseSnow.VY += (looseSnow.FallSpeed - looseSnow.VY) * num;
					looseSnow.VX *= num2;
					looseSnow.X += (float)Math.Sin((double)looseSnow.Age * 2.8 + (double)looseSnow.FallSpeed) * 9f * dt;
				}
				else
				{
					looseSnow.VY += 320f * dt;
				}
				looseSnow.X += looseSnow.VX * dt;
				looseSnow.Y += looseSnow.VY * dt;
				Surface surface = null;
				float num6 = float.MaxValue;
				foreach (Surface surface2 in Surfaces)
				{
					if (!(surface2.Id == looseSnow.ShakenFrom) && surface2.Accumulates && !(looseSnow.X < (float)surface2.Bounds.Left) && !(looseSnow.X > (float)surface2.Bounds.Right))
					{
						float num7 = surface2.Top(looseSnow.X);
						if (num5 <= num7 + 1f && looseSnow.Y + looseSnow.Radius >= num7 && num7 < num6 && surface2.Visible(looseSnow.X, num7 - 0.5f))
						{
							surface = surface2;
							num6 = num7;
						}
					}
				}
				if (surface != null)
				{
					looseSnow.Y = num6;
					surface.DepositMass(looseSnow.X, looseSnow.Mass);
					Burst(looseSnow);
				}
				if (surface != null || looseSnow.Y > num3 || looseSnow.Age > 45f)
				{
					Loose.RemoveAt(num4);
				}
			}
		}

		private void StepPlows(float dt)
		{
			foreach (var vehicle in Plows)
				if (Display.SnowOn(vehicle.Screen.Id)) vehicle.Step(dt, Spray, random);
			for (int num9 = Spray.Count - 1; num9 >= 0; num9--)
			{
				PlowSpray plowSpray = Spray[num9];
				plowSpray.Age += dt;
				plowSpray.VY += 220f * dt;
				plowSpray.X += plowSpray.VX * dt;
				plowSpray.Y += plowSpray.VY * dt;
				if (plowSpray.Age >= plowSpray.Life || plowSpray.Y >= (float)plowSpray.Screen.SnowArea.Bottom)
				{
					Spray.RemoveAt(num9);
				}
			}
		}

		internal void UpdateAnchors()
		{
			foreach (Ball ball in Balls)
			{
				if (ball.Support == null)
				{
					continue;
				}
				if (!Surfaces.Contains(ball.Support))
				{
					ball.Support = null;
					ball.V = 0f;
					continue;
				}
				ball.X = (float)ball.Support.Bounds.Left + ball.Offset;
				if (ball.X - ball.R < (float)ball.Support.Bounds.Left || ball.X + ball.R > (float)ball.Support.Bounds.Right)
				{
					ball.Support = null;
					ball.V = 0f;
				}
				else
				{
					ball.Y = ball.Support.Top(ball.X) - ball.R;
				}
			}
			foreach (Ball ball2 in Balls)
			{
				Follow(ball2, 0);
			}
		}

		private void Follow(Ball b, int depth)
		{
			if (b.Parent != null && depth <= 3)
			{
				Follow(b.Parent, depth + 1);
				b.X = b.Parent.X;
				b.Y = b.Parent.Y - b.Parent.R - b.R + 2f;
			}
		}

		private void PushSettledSnow()
		{
			if (!PointerPresent || !InSnowArea(Pointer) || Held != null)
			{
				trackedSnowPointer = false;
				return;
			}
			if (!trackedSnowPointer)
			{
				lastSnowPointer = Pointer;
				trackedSnowPointer = true;
				return;
			}
			float num = Pointer.X - lastSnowPointer.X;
			float num2 = Pointer.Y - lastSnowPointer.Y;
			float num3 = (float)Math.Sqrt(num * num + num2 * num2);
			if (num3 > 0.05f && num3 < 180f)
			{
				int num4 = Math.Max(1, (int)Math.Ceiling(num3 / 4f));
				for (int i = 1; i <= num4; i++)
				{
					PointF pointF = new PointF(lastSnowPointer.X + num * (float)i / (float)num4, lastSnowPointer.Y + num2 * (float)i / (float)num4);
					if (!InSnowArea(pointF))
					{
						continue;
					}
					foreach (Surface surface in Surfaces)
					{
						PushSurface(surface, pointF, num, num2, num3 / (float)num4);
					}
				}
			}
			lastSnowPointer = Pointer;
		}

		private void PushSurface(Surface s, PointF point, float motionX, float motionY, float distance)
		{
			if (point.X < (float)s.Bounds.Left - 24f || point.X > (float)s.Bounds.Right + 24f || Math.Abs(point.Y - (float)s.Bounds.Top) > 54f)
			{
				return;
			}
			Array.Clear(s.Push, 0, s.Push.Length);
			float num = (float)s.Bounds.Width / (float)Math.Max(1, s.Snow.Length - 1);
			float num2 = (s.Ground ? 44 : 40);
			int num3 = Math.Max(0, (int)Math.Floor((point.X - 24f - (float)s.Bounds.Left) / Math.Max(1f, num)));
			int num4 = Math.Min(s.Snow.Length - 1, (int)Math.Ceiling((point.X + 24f - (float)s.Bounds.Left) / Math.Max(1f, num)));
			float num5 = 0f;
			float num6 = 0f;
			float num7 = 0f;
			float num8 = 0f;
			for (int i = num3; i <= num4; i++)
			{
				float num9 = (float)s.Bounds.Left + (float)i * num;
				float num10 = s.Snow[i];
				float num11 = (float)s.Bounds.Top - num10 * 0.7f;
				float num12 = (num9 - point.X) * (num9 - point.X) + (num11 - point.Y) * (num11 - point.Y);
				if (num10 <= 0.01f || num12 >= 576f || !s.Visible(num9, (float)s.Bounds.Top - num10 - 0.5f))
				{
					continue;
				}
				int num13 = ((!(Math.Abs(motionX) > 0.2f)) ? ((!(num9 < point.X)) ? 1 : (-1)) : ((motionX > 0f) ? 1 : (-1)));
				if (!s.Ground && point.X < (float)s.Bounds.Left + 24f)
				{
					num13 = -1;
				}
				else if (!s.Ground && point.X > (float)s.Bounds.Right - 24f)
				{
					num13 = 1;
				}
				float num14 = Math.Min(Math.Max(0f, num10 + Math.Min(0f, s.Push[i])), num10 * Math.Min(0.4f, distance * 0.09f) * (1f - num12 / 576f));
				float num15 = 0f;
				if (!s.Ground && motionY > 0.4f && motionY > Math.Abs(motionX) * 0.8f && point.Y >= (float)(s.Bounds.Top - 16) && point.Y <= (float)(s.Bounds.Top + 12))
				{
					float num16 = num14 * 0.7f;
					num7 += num16;
					num8 += num16 * num9;
					num15 += num16;
					num14 -= num16;
				}
				for (int j = -1; j <= 1; j++)
				{
					int num17 = i + num13 * 3 + j;
					if (!s.Ground && (num17 <= 0 || num17 >= s.Snow.Length - 1))
					{
						float num18 = num14 * ((j == 0) ? 0.5f : 0.25f);
						if (num17 <= 0)
						{
							num5 += num18;
						}
						else
						{
							num6 += num18;
						}
						num15 += num18;
					}
					else if (num17 >= 0 && num17 < s.Snow.Length)
					{
						float x = (float)s.Bounds.Left + (float)num17 * num;
						if (s.Visible(x, s.Top(x) - 0.5f) && InSnowArea(new PointF(x, s.Bounds.Top - 1)))
						{
							float num19 = Math.Min(Math.Max(0f, num2 - s.Snow[num17] - s.Push[num17]), num14 * ((j == 0) ? 0.5f : 0.25f));
							s.Push[num17] += num19;
							num15 += num19;
						}
					}
				}
				s.Push[i] -= num15;
			}
			for (int k = 0; k < s.Snow.Length; k++)
			{
				s.Snow[k] += s.Push[k];
			}
			EmitBrushedSnow(s, s.Bounds.Left - 5, num5, -1);
			EmitBrushedSnow(s, s.Bounds.Right + 5, num6, 1);
			if (num7 > 0f)
			{
				EmitBrushedSnow(s, num8 / num7, num7, 0);
			}
		}

		private void EmitBrushedSnow(Surface source, float x, float mass, int direction)
		{
			if (mass <= 0f)
			{
				return;
			}
			foreach (ScreenArea screen in Screens)
			{
				if (x >= (float)(screen.Bounds.Left - 8) && x <= (float)(screen.Bounds.Right + 8) && source.Bounds.Top >= screen.Bounds.Top && source.Bounds.Top < screen.Bounds.Bottom)
				{
					x = Math.Max(screen.SnowArea.Left + 4, Math.Min(screen.SnowArea.Right - 4, x));
					break;
				}
			}
			Loose.Add(new LooseSnow
			{
				X = x,
				Y = source.Bounds.Top + 1,
				VX = (float)direction * (18f + (float)random.NextDouble() * 18f),
				VY = 20f + (float)random.NextDouble() * 15f,
				Radius = Math.Min(3.5f, 1f + (float)Math.Sqrt(mass) * 0.5f),
				Mass = mass,
				FrontWindows = source.FrontWindows
			});
		}

		internal void Step(float dt)
		{
			time += dt;
			foreach (Surface surface3 in Surfaces)
			{
				if (!surface3.Ground)
				{
					surface3.AnimateGarland(dt);
				}
			}
			EvolveSnow(dt);
			StepDust(dt);
			PushSettledSnow();
			StepFlakes(dt);
			StepLoose(dt);
			StepPlows(dt);
			UpdateAnchors();
			StepBalls(dt);
			UpdateAnchors();
		}
		private void StepFlakes(float dt)
		{
			double wind = Math.Sin(time * 0.22) * 14.0;
			for (int i = 0; i < Flakes.Count; i++)
			{
				Flake flake = Flakes[i];
				float y = flake.Y;
				flake.X += (float)(wind + Math.Sin(time * 0.75 + (double)flake.Phase) * 8.0) * dt;
				flake.Y += flake.Speed * dt;
				if (flake.Y + flake.Radius < (float)flake.Screen.SnowArea.Top)
				{
					continue;
				}
				if (PointerPresent)
				{
					float num = flake.X - Pointer.X;
					float num2 = flake.Y - Pointer.Y;
					float num3 = num * num + num2 * num2;
					if (num3 < 1764f)
					{
						float num4 = (1f - (float)Math.Sqrt(num3) / 42f) * 65f * dt;
						flake.X += (float)((!(num < 0f)) ? 1 : (-1)) * num4;
					}
				}
				Surface surface = null;
				float num5 = float.MaxValue;
				foreach (Surface surface4 in Surfaces)
				{
					if (surface4.Accumulates && y <= surface4.Bounds.Top && flake.X >= surface4.Bounds.Left + 3 && flake.X <= surface4.Bounds.Right - 3)
					{
						float num6 = surface4.Top(flake.X);
						if (y <= num6 && flake.Y >= num6 && num6 < num5 && surface4.Visible(flake.X, num6 - 0.5f))
						{
							surface = surface4;
							num5 = num6;
						}
					}
				}
				surface?.Deposit(flake.X, 0.38f);
				Rectangle snowArea = flake.Screen.SnowArea;
				if (surface != null || flake.Y > (float)(snowArea.Bottom + 8) || flake.X < (float)(snowArea.Left - 8) || flake.X > (float)(snowArea.Right + 8))
				{
					Flakes[i] = NewFlake(flake.Screen, scatter: false);
				}
			}
		}
		private void StepBalls(float dt)
		{
			foreach (Ball ball in Balls)
			{
				if (ball == Held || ball.Rest)
				{
					continue;
				}
				float num7 = ball.Y + ball.R;
				ball.V += 400f * dt;
				ball.Y += ball.V * dt;
				bool flag = false;
				foreach (Ball ball2 in Balls)
				{
					if (!HasChild(ball) && ball2 != ball && ball2 != Held && ball2.Rest && ball2.Level < 2 && !IsDescendant(ball2, ball) && !HasChild(ball2))
					{
						float num8 = ball2.Y - ball2.R;
						if (Math.Abs(ball.X - ball2.X) < Math.Max(30f, ball2.R * 1.4f) && num7 <= num8 + 5f && ball.Y + ball.R >= num8)
						{
							Attach(ball, ball2);
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					Surface surface2 = FindLanding(ball.X, ball.R, num7, ball.Y + ball.R);
					if (surface2 != null)
					{
						ball.Support = surface2;
						ball.Offset = ball.X - (float)surface2.Bounds.Left;
						ball.Y = surface2.Top(ball.X) - ball.R;
						ball.V = 0f;
						TryStackNearby(ball);
					}
					if (ball.Y > MaxBottom() + 200f)
					{
						ReturnToScreen(ball);
					}
				}
			}

		}

		internal Surface FindLanding(float x, float radius, float from, float to)
		{
			Surface result = null;
			float num = float.MaxValue;
			foreach (Surface surface in Surfaces)
			{
				float num2 = surface.Top(x);
				if (from <= num2 + 1f && to >= num2 && num2 < num && surface.Supports(x, radius))
				{
					result = surface;
					num = num2;
				}
			}
			return result;
		}

		private float MaxBottom()
		{
			float num = 0f;
			foreach (ScreenArea screen in Screens)
			{
				num = Math.Max(num, screen.Bounds.Bottom);
			}
			return num;
		}

		private void ReturnToScreen(Ball b)
		{
			if (Screens.Count == 0)
			{
				return;
			}
			ScreenArea screenArea = Screens[0];
			float num = float.MaxValue;
			foreach (ScreenArea screen in Screens)
			{
				float dx = b.X - Math.Max(screen.Work.Left, Math.Min(screen.Work.Right, b.X));
				float dy = b.Y - Math.Max(screen.Work.Top, Math.Min(screen.Work.Bottom, b.Y));
				float num2 = dx * dx + dy * dy;
				if (num2 < num)
				{
					num = num2;
					screenArea = screen;
				}
			}
			b.X = Math.Max((float)screenArea.Work.Left + b.R + 4f, Math.Min((float)screenArea.Work.Right - b.R - 4f, b.X));
			b.Y = (float)screenArea.Work.Top - b.R;
			b.V = 0f;
		}

		private bool HasChild(Ball parent)
		{
			foreach (Ball ball in Balls)
			{
				if (ball.Parent == parent)
				{
					return true;
				}
			}
			return false;
		}

		private bool IsDescendant(Ball b, Ball parent)
		{
			for (Ball parent2 = b.Parent; parent2 != null; parent2 = parent2.Parent)
			{
				if (parent2 == parent)
				{
					return true;
				}
			}
			return false;
		}

		private void Attach(Ball b, Ball parent)
		{
			b.Support = null;
			b.Parent = parent;
			b.R = parent.R * 0.73f;
			b.V = 0f;
			b.X = parent.X;
			b.Y = parent.Y - parent.R - b.R + 2f;
		}

		private void TryStackNearby(Ball b)
		{
			if (HasChild(b))
			{
				return;
			}
			foreach (Ball ball2 in Balls)
			{
				if (ball2 != b && ball2 != Held && ball2.Rest && ball2.Level < 2 && !HasChild(ball2) && !IsDescendant(ball2, b))
				{
					Ball ball = ball2;
					while (ball.Parent != null)
					{
						ball = ball.Parent;
					}
					if (ball.Support == b.Support && Math.Abs(b.X - ball.X) < Math.Max(48f, ball.R * 2f))
					{
						Attach(b, ball2);
						break;
					}
				}
			}
		}

		internal void Pick(PointF point)
		{
			for (int num = Balls.Count - 1; num >= 0; num--)
			{
				Ball ball = Balls[num];
				if (Math.Pow(point.X - ball.X, 2.0) + Math.Pow(point.Y - ball.Y, 2.0) <= (double)(ball.R * ball.R))
				{
					Held = ball;
					ball.Parent = null;
					ball.Support = null;
					ball.V = 0f;
					return;
				}
			}
			Held = new Ball
			{
				X = point.X,
				Y = point.Y,
				R = 9f
			};
			Balls.Add(Held);
		}

		internal void Drag(PointF point, float distance)
		{
			if (Held == null)
			{
				return;
			}
			Held.X = point.X;
			Held.Y = point.Y;
			Held.V = 0f;
			foreach (Surface surface in Surfaces)
			{
				if (!(Math.Abs(point.Y - surface.Top(point.X)) > Held.R + 16f) && surface.Visible(point.X, surface.Top(point.X) - 1f))
				{
					float val = surface.HeightAt(point.X);
					float num = Math.Min(val, distance * 0.12f);
					surface.Deposit(point.X, 0f - num);
					Held.R = Math.Min(38f, (float)Math.Sqrt(Held.R * Held.R + num * 13f));
				}
			}
			UpdateAnchors();
		}

		internal void Release()
		{
			if (Held != null)
			{
				Ball held = Held;
				Held = null;
				held.V = 0f;
				Surface surface = FindLanding(held.X, held.R, held.Y - held.R - 14f, held.Y + held.R + 14f);
				if (surface != null)
				{
					held.Support = surface;
					held.Offset = held.X - (float)surface.Bounds.Left;
					held.Y = surface.Top(held.X) - held.R;
					TryStackNearby(held);
				}
				UpdateAnchors();
			}
		}

		internal Surface Anchor(Ball b)
		{
			while (b.Parent != null)
			{
				b = b.Parent;
			}
			return b.Support;
		}

		internal bool InSnowArea(PointF p)
		{
			bool flag = false;
			foreach (ScreenArea screen in Screens)
			{
				if (((RectangleF)screen.SnowArea).Contains(p))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			foreach (Rectangle taskbar in Taskbars)
			{
				if (((RectangleF)taskbar).Contains(p))
				{
					return false;
				}
			}
			foreach (Rectangle fullScreenCover in FullScreenCovers)
			{
				if (((RectangleF)fullScreenCover).Contains(p))
				{
					return false;
				}
			}
			return true;
		}

		internal bool HitTest(PointF p)
		{
			if (!InSnowArea(p))
			{
				return false;
			}
			foreach (Ball ball in Balls)
			{
				if (Math.Pow(p.X - ball.X, 2.0) + Math.Pow(p.Y - ball.Y, 2.0) > (double)(ball.R * ball.R))
				{
					continue;
				}
				Surface surface = Anchor(ball);
				bool flag = false;
				if (surface != null)
				{
					foreach (Rectangle cover in surface.Covers)
					{
						if (((RectangleF)cover).Contains(p))
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					return true;
				}
			}
			foreach (Surface surface2 in Surfaces)
			{
				float num = (surface2.HeightAt(p.X - 4f) + 2f * surface2.HeightAt(p.X) + surface2.HeightAt(p.X + 4f)) / 4f;
				if (num > 0.12f && p.Y >= (float)surface2.Bounds.Top - num - 0.5f && p.Y < (float)surface2.Bounds.Top + 0.5f && surface2.Visible(p.X, p.Y))
				{
					return true;
				}
			}
			return false;
		}

		internal void Clear()
		{
			foreach (Surface surface in Surfaces)
			{
				Array.Clear(surface.Snow, 0, surface.Snow.Length);
			}
			foreach (Snowplow plow in Plows)
			{
				plow.Reset();
			}
			Held = null;
			Balls.Clear();
			Loose.Clear();
			Spray.Clear();
			Dust.Clear();
			trackedSnowPointer = false;
		}
	}
}





