using Microsoft.Xna.Framework;

namespace SpaceDrift.Components
{
	// Depth is the parallax factor: 1 scrolls with the world, lower is farther.
	public struct Star
	{
		public Color Color;
		public float Size;
		public float Depth;
	}
}
