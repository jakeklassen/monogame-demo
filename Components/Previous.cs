using Microsoft.Xna.Framework;

namespace CherryBomb.Components
{
	// Snapshot of the transform at the previous fixed step, for render
	// interpolation. A distinct type from Transform so both can live on one entity.
	public struct Previous(Vector2 position, float rotation)
	{
		public Vector2 Position = position;
		public float Rotation = rotation;
	}
}
