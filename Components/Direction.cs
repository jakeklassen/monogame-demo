using System;

namespace Components
{
	public class Direction
	{
		public int X { get; set; }
		public int Y { get; set; }

		public Direction()
		{
			X = 0;
			Y = 0;
		}

		public Direction(int x, int y)
		{
			X = x;
			Y = y;
		}

		public Direction(Direction direction)
		{
			X = direction.X;
			Y = direction.Y;
		}

		public static Direction Random()
		{
			Random random = new();

			return new Direction(
				x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
				y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
			);
		}
	}
}
