using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinterDesktop
{
	internal sealed class Surface
	{
		internal string Id;

		internal Rectangle Bounds;

		internal bool Ground;

		internal bool Accumulates = true;

		internal float[] Snow;

		internal float[] Push;

		internal int GarlandSpans;

		internal int GarlandWidth;

		internal float GarlandSwing;

		internal float GarlandVelocity;

		internal readonly float[] CordDrop = new float[16];

		internal readonly float[] CordSpeed = new float[16];

		internal readonly float[] CordSway = new float[16];

		internal readonly float[] CordSwaySpeed = new float[16];

		internal List<Rectangle> Covers = new List<Rectangle>();

		internal List<string> FrontWindows = new List<string>();

		internal float GarlandSag => Math.Max(9f, Math.Min(36f, (float)(20 * GarlandWidth) / (float)Math.Max(1, Bounds.Width)));

		internal void NudgeGarland(float dx, float dy, float widthChange)
		{
			for (int i = 0; i < GarlandSpans; i++)
			{
				float num = 0.75f + 0.25f * (float)Math.Sin((double)i * 1.7);
				CordSpeed[i] = Math.Max(-180f, Math.Min(180f, CordSpeed[i] - dy * 1.5f - widthChange * 0.65f * num));
				CordSwaySpeed[i] = Math.Max(-160f, Math.Min(160f, CordSwaySpeed[i] - dx * 1.8f + widthChange * ((float)i / (float)Math.Max(1, GarlandSpans - 1) - 0.5f)));
			}
		}

		internal PointF CordPoint(int section, float t)
		{
			float num = (float)Bounds.Width / (float)GarlandSpans;
			return new PointF((float)Bounds.Left + ((float)section + t) * num + (float)Math.Sin(Math.PI * (double)t) * CordSway[section], (float)(Bounds.Top + 3) + CordDrop[section] * 4f * t * (1f - t));
		}

		internal void AnimateGarland(float dt)
		{
			GarlandVelocity += (-55f * GarlandSwing - 9f * GarlandVelocity) * dt;
			GarlandSwing += GarlandVelocity * dt;
			for (int i = 0; i < GarlandSpans; i++)
			{
				float num = 32 + i % 3 * 5;
				CordSpeed[i] += ((GarlandSag - CordDrop[i]) * num - 5.5f * CordSpeed[i]) * dt;
				CordDrop[i] = Math.Max(4f, Math.Min(43f, CordDrop[i] + CordSpeed[i] * dt));
				CordSwaySpeed[i] += ((0f - num) * CordSway[i] - 4.5f * CordSwaySpeed[i]) * dt;
				CordSway[i] = Math.Max(-16f, Math.Min(16f, CordSway[i] + CordSwaySpeed[i] * dt));
			}
		}

		internal Surface(string id, Rectangle rect, bool ground)
		{
			Id = id;
			Ground = ground;
			GarlandWidth = rect.Width;
			GarlandSpans = Math.Max(1, Math.Min(16, (int)Math.Ceiling((double)rect.Width / 140.0)));
			Resize(rect);
			for (int i = 0; i < GarlandSpans; i++)
			{
				CordDrop[i] = GarlandSag;
			}
		}

		internal void Resize(Rectangle rect)
		{
			int num = Math.Max(2, (rect.Width + 5) / 6 + 1);
			if (Snow == null || Snow.Length != num)
			{
				float[] array = new float[num];
				if (Snow != null)
				{
					for (int i = 0; i < num; i++)
					{
						array[i] = Snow[Math.Min(Snow.Length - 1, (int)((long)i * (long)(Snow.Length - 1) / (num - 1)))];
					}
				}
				Snow = array;
				Push = new float[num];
			}
			Bounds = rect;
		}

		internal float HeightAt(float x)
		{
			float num = Math.Max(0f, Math.Min(Snow.Length - 1, (x - (float)Bounds.Left) / (float)Math.Max(1, Bounds.Width) * (float)(Snow.Length - 1)));
			int num2 = (int)num;
			int num3 = Math.Min(Snow.Length - 1, num2 + 1);
			return Snow[num2] + (Snow[num3] - Snow[num2]) * (num - (float)num2);
		}

		internal float Top(float x)
		{
			return (float)Bounds.Top - HeightAt(x);
		}

		internal bool Visible(float x, float y)
		{
			if (x < (float)(Bounds.Left + 3) || x > (float)(Bounds.Right - 3))
			{
				return false;
			}
			foreach (Rectangle cover in Covers)
			{
				if (x >= (float)cover.Left && x < (float)cover.Right && y >= (float)cover.Top && y < (float)cover.Bottom)
				{
					return false;
				}
			}
			return true;
		}

		internal bool Supports(float x, float r)
		{
			if (x - r >= (float)(Bounds.Left + 3) && x + r <= (float)(Bounds.Right - 3) && Visible(x, Top(x) - 1f) && Visible(x - r * 0.6f, Top(x) - 1f))
			{
				return Visible(x + r * 0.6f, Top(x) - 1f);
			}
			return false;
		}

		internal void Deposit(float x, float amount)
		{
			if (Accumulates)
			{
				float num = (x - (float)Bounds.Left) / (float)Math.Max(1, Bounds.Width) * (float)(Snow.Length - 1);
				int num2 = (int)num;
				for (int i = Math.Max(0, num2 - 3); i <= Math.Min(Snow.Length - 1, num2 + 3); i++)
				{
					float num3 = (float)Math.Exp(0.0 - Math.Pow((double)((float)i - num) / 1.7, 2.0));
					Snow[i] = Math.Min(Math.Max(Ground ? 24 : 22, Snow[i]), Math.Max(0f, Snow[i] + amount * num3));
				}
			}
		}

		internal void DepositMass(float x, float mass)
		{
			if (!Accumulates)
			{
				return;
			}
			float num = (x - (float)Bounds.Left) / (float)Math.Max(1, Bounds.Width) * (float)(Snow.Length - 1);
			int num2 = (int)num;
			float num3 = 0f;
			for (int i = Math.Max(0, num2 - 3); i <= Math.Min(Snow.Length - 1, num2 + 3); i++)
			{
				num3 += (float)Math.Exp(0.0 - Math.Pow((double)((float)i - num) / 1.7, 2.0));
			}
			if (num3 > 0f)
			{
				for (int j = Math.Max(0, num2 - 3); j <= Math.Min(Snow.Length - 1, num2 + 3); j++)
				{
					Snow[j] += mass * (float)Math.Exp(0.0 - Math.Pow((double)((float)j - num) / 1.7, 2.0)) / num3;
				}
			}
		}
	}
}
