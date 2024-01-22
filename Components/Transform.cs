using Microsoft.Xna.Framework;

namespace Components
{
	public class Transform(Vector2 position, float rotation, Vector2 scale)
	{
		public Vector2 Position { get; set; } = position;
		public float Rotation { get; set; } = rotation;
		public Vector2 Scale { get; set; } = scale;
	}
}
