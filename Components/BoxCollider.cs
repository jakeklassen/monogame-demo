using Microsoft.Xna.Framework;

namespace Components
{
	public class BoxCollider
	{
		public int Width { get; private set; } = 0;
		public int Height { get; private set; } = 0;
		public Vector2 Offset { get; private set; } = Vector2.Zero;

		public BoxCollider(int width, int height, Vector2 offset = default)
		{
			Width = width;
			Height = height;
			Offset = offset;
		}
	}
}
