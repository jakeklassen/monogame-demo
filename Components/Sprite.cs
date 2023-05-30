using Microsoft.Xna.Framework;

namespace Components
{
	public class Sprite
	{
		public Rectangle CurrentFrame { get; set; }
		public float Opacity { get; set; } = 1f;

		public Sprite(Rectangle frame, float opacity = 1f)
		{
			CurrentFrame = frame;
			Opacity = opacity;
		}
	}
}
