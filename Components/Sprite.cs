using Microsoft.Xna.Framework;

namespace Components
{
	public class Sprite(Rectangle frame, float opacity = 1f)
	{
		public Rectangle CurrentFrame { get; set; } = frame;
		public float Opacity { get; set; } = opacity;
	}
}
