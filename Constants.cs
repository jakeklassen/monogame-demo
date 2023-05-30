// public enum Pico8Color
// {
//   Color0 = 0x000000,
//   Color1 = 0x1D2B53,
//   Color2 = 0x7E2553,
//   Color3 = 0x008751,
//   Color4 = 0xAB5236,
//   Color5 = 0x5F574F,
//   Color6 = 0xC2C3C7,
//   Color7 = 0xFFF1E8,
//   Color8 = 0xFF004D,
//   Color9 = 0xFFA300,
//   Color10 = 0xFFEC27,
//   Color11 = 0x00E436,
//   Color12 = 0x29ADFF,
//   Color13 = 0x83769C,
//   Color14 = 0xFF77A8,
//   Color15 = 0xFFCCAA
// }

using Components;

namespace CherryBomb
{
	public class Pico8Color
	{
		public static readonly Color Color0 = new(0x00, 0x00, 0x00, 0xFF);
		public static readonly Color Color1 = new(0x1D, 0x2B, 0x53, 0xFF);
		public static readonly Color Color2 = new(0x7E, 0x25, 0x53, 0xFF);
		public static readonly Color Color3 = new(0x00, 0x87, 0x51, 0xFF);
		public static readonly Color Color4 = new(0xAB, 0x52, 0x36, 0xFF);
		public static readonly Color Color5 = new(0x5F, 0x57, 0x4F, 0xFF);
		public static readonly Color Color6 = new(0xC2, 0xC3, 0xC7, 0xFF);
		public static readonly Color Color7 = new(0xFF, 0xF1, 0xE8, 0xFF);
		public static readonly Color Color8 = new(0xFF, 0x00, 0x4D, 0xFF);
		public static readonly Color Color9 = new(0xFF, 0xA3, 0x00, 0xFF);
		public static readonly Color Color10 = new(0xFF, 0xEC, 0x27, 0xFF);
		public static readonly Color Color11 = new(0x00, 0xE4, 0x36, 0xFF);
		public static readonly Color Color12 = new(0x29, 0xAD, 0xFF, 0xFF);
		public static readonly Color Color13 = new(0x83, 0x76, 0x9C, 0xFF);
		public static readonly Color Color14 = new(0xFF, 0x77, 0xA8, 0xFF);
		public static readonly Color Color15 = new(0xFF, 0xCC, 0xAA, 0xFF);
	}

	public class CollisionMasks
	{
		public const int Player = 1 << 0;
		public const int PlayerProjectile = 1 << 1;
		public const int Enemy = 1 << 2;
		public const int EnemyProjectile = 1 << 3;
		public const int Pickup = 1 << 4;

		public int Value { get; set; }

		public CollisionMasks(int value)
		{
			Value = value;
		}
	}
}
