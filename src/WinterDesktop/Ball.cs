namespace WinterDesktop
{
	internal sealed class Ball
	{
		internal float X;

		internal float Y;

		internal float R;

		internal float V;

		internal float Offset;

		internal Surface Support;

		internal Ball Parent;

		internal int Level
		{
			get
			{
				if (Parent != null)
				{
					return Parent.Level + 1;
				}
				return 0;
			}
		}

		internal bool Rest
		{
			get
			{
				if (Support == null)
				{
					return Parent != null;
				}
				return true;
			}
		}
	}
}
