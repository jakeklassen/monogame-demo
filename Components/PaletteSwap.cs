namespace CherryBomb.Components
{
	// Marks a sprite to be drawn with the boss hurt palette swap
	// (Color3 -> Color8, Color11 -> Color14) instead of normally — matching the
	// TS source's sprite.paletteSwaps during the boss hurt window. Drawn by
	// PaletteSwapRenderingSystem; SpriteRenderingSystem skips entities that have
	// it (WithNone<PaletteSwap>).
	public struct PaletteSwap { }
}
