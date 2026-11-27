namespace WinterDesktop
{
	internal sealed class Snowplow
	{
		internal Surface Ground;

		internal ScreenArea Screen;

		internal double SnowSeconds;

		internal bool Active;

		internal float X;

		internal float Travel;

		internal Snowplow(Surface ground, ScreenArea screen)
		{
			Ground = ground;
			Screen = screen;
		}
	}
}
