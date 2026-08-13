using Microsoft.Xna.Framework;

namespace CherryBomb
{
	public readonly struct PlanetPalette(Color dark, Color @base, Color light)
	{
		public readonly Color Dark = dark;
		public readonly Color Base = @base;
		public readonly Color Light = light;
	}

	// Ported from space-drift/palette.ts. Colors are stored as XNA Colors (bytes)
	// rather than the source's linear 0..1 triples, which is the natural fit for
	// SpriteBatch tinting. Scale() reproduces toHex(color, mul): a per-channel
	// multiply used for star brightness pulses.
	public static class Palette
	{
		public static readonly Color Black = new(0, 0, 0);
		public static readonly Color DarkBlue = new(29, 43, 83);
		public static readonly Color DarkPurple = new(126, 37, 83);
		public static readonly Color DarkGreen = new(0, 135, 81);
		public static readonly Color Brown = new(171, 82, 54);
		public static readonly Color DarkGray = new(95, 87, 79);
		public static readonly Color LightGray = new(194, 195, 199);
		public static readonly Color White = new(255, 241, 232);
		public static readonly Color Red = new(255, 0, 77);
		public static readonly Color Orange = new(255, 163, 0);
		public static readonly Color Yellow = new(255, 236, 39);
		public static readonly Color Green = new(0, 228, 54);
		public static readonly Color Blue = new(41, 173, 255);
		public static readonly Color Lavender = new(131, 118, 156);
		public static readonly Color Pink = new(255, 119, 168);
		public static readonly Color Peach = new(255, 204, 170);

		// Deep-space background — a near-black blue.
		public static readonly Color SpaceColor = new(6, 7, 18);

		// A few hand-picked planet "types": {shadow, midtone, lit}.
		public static readonly PlanetPalette[] PlanetPalettes =
		[
			new(DarkGray, Brown, Orange), // rocky
			new(DarkBlue, Blue, LightGray), // ocean
			new(Brown, Orange, Yellow), // gas giant
			new(DarkPurple, Lavender, LightGray), // ice
			new(DarkGreen, Green, Yellow), // verdant
			new(DarkPurple, Pink, Peach), // exotic
		];

		// Per-channel multiply, clamped — the equivalent of toHex(color, mul).
		public static Color Scale(Color c, float mul)
		{
			int r = (int)(c.R * mul);
			int g = (int)(c.G * mul);
			int b = (int)(c.B * mul);
			return new Color(
				r < 0 ? 0
					: r > 255 ? 255
					: r,
				g < 0 ? 0
					: g > 255 ? 255
					: g,
				b < 0 ? 0
					: b > 255 ? 255
					: b
			);
		}
	}
}
