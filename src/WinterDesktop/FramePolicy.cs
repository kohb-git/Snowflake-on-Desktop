namespace WinterDesktop
{
	internal static class FramePolicy
	{
		internal static int Interval(int mode, bool onBattery, bool autoEconomy, bool suspended, bool covered, bool paused)
		{
			if (suspended || covered)
			{
				return 250;
			}
			if (paused || mode == 1 || (autoEconomy && onBattery))
			{
				return 33;
			}
			if (mode != 2)
			{
				return 16;
			}
			return 8;
		}
	}
}
