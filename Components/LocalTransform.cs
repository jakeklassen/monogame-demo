using Microsoft.Xna.Framework;

namespace CherryBomb.Components
{
	// A positional offset applied relative to a parent entity's Transform.
	// Paired with Parent and driven by LocalTransformSystem each frame.
	public class LocalTransform(Vector2 position)
	{
		public Vector2 Position { get; set; } = position;
	}
}
