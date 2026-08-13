using Microsoft.Xna.Framework;

namespace SpaceDrift.Components
{
	// Rotation is in DEGREES, 0 = up (-y). Matches space-drift/entity.ts Transform.
	public struct Transform(Vector2 position, float rotation)
	{
		public Vector2 Position = position;
		public float Rotation = rotation;
	}
}
