namespace CherryBomb.Components
{
	// Requests a 1px dilation outline rendered around the entity's Sprite. The
	// Color is animated by SpriteOutlineAnimation and consumed by
	// SpriteOutlineRenderingSystem (which pre-bakes a tinted outline mask per
	// frame+color, reusing the cached-mask technique from FlashRenderingSystem).
	// Source: sprite-outline-rendering-system.ts.
	public class SpriteOutline
	{
		public Color Color { get; set; }
	}
}
