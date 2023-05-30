using XnaColor = Microsoft.Xna.Framework.Color;

namespace Components
{
	public class Color
	{
		public int R { get; set; }
		public int G { get; set; }
		public int B { get; set; }
		public int A { get; set; }
		public XnaColor XnaColor => new(R, G, B, A);

		public Color(int r, int g, int b, int a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public Color(int color)
		{
			R = (color >> 24) & 0xFF;
			G = (color >> 16) & 0xFF;
			B = (color >> 8) & 0xFF;
			A = color & 0xFF;
		}
	}
}
