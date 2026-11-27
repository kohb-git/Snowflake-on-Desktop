using System.Drawing;

namespace WinterDesktop
{
	internal sealed class ScreenArea
	{
		internal string Id;

		internal Rectangle Bounds;

		internal Rectangle Work;

		internal Rectangle SnowArea;

		internal ScreenArea(string id, Rectangle bounds, Rectangle work)
		{
			Id = id;
			Bounds = bounds;
			Work = work;
			SnowArea = Rectangle.Intersect(bounds, work);
		}
	}
}
