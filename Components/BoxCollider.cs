using Microsoft.Xna.Framework;

namespace Components
{
	public class BoxCollider(int width, int height, Vector2 offset = default)
	{
		public int Width { get; private set; } = width;
		public int Height { get; private set; } = height;
		public Vector2 Offset { get; private set; } = offset;
	}
}
