using Microsoft.Xna.Framework;

namespace Components
{
	public class Transform
	{
		public Vector2 Position { get; set; }
		public float Rotation { get; set; }
		public Vector2 Scale { get; set; }

		public Transform(Vector2 position, float rotation, Vector2 scale)
		{
			Position = position;
			Rotation = rotation;
			Scale = scale;
		}
	}
}