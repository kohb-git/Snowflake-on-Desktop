using System.Drawing;

namespace WinterDesktop
{
	public sealed class WindowInfo
	{
		public string Id;

		public Rectangle Bounds;

		public bool Shelf;

		public bool Maximized;

		public bool SuppressScreen = true;

		public WindowInfo(string id, Rectangle rect, bool shelf, bool maximized = false)
		{
			Id = id;
			Bounds = rect;
			Shelf = shelf;
			Maximized = maximized;
		}
	}
}
