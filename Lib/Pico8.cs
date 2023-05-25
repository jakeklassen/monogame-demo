using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lib
{
	public static class Pico8Extensions
	{
		public static Texture2D Circ(GraphicsDevice graphicsDevice, float radius, Color color)
		{
			int diameter = (int)(radius * 2);
			int textureSize = diameter + 1;
			Texture2D texture = new(graphicsDevice, textureSize, textureSize);

			Color[] data = new Color[textureSize * textureSize];

			int x = (int)radius;
			int y = 0;
			int radiusError = 1 - x;

			while (x >= y)
			{
				data[(int)(((radius + y) * textureSize) + radius + x)] = color;
				data[(int)(((radius - y) * textureSize) + radius + x)] = color;
				data[(int)(((radius + y) * textureSize) + radius - x)] = color;
				data[(int)(((radius - y) * textureSize) + radius - x)] = color;
				data[(int)(((radius + x) * textureSize) + radius + y)] = color;
				data[(int)(((radius - x) * textureSize) + radius + y)] = color;
				data[(int)(((radius + x) * textureSize) + radius - y)] = color;
				data[(int)(((radius - x) * textureSize) + radius - y)] = color;

				y++;

				if (radiusError < 0)
				{
					radiusError += (2 * y) + 1;
				}
				else
				{
					x--;
					radiusError += (2 * (y - x)) + 1;
				}
			}

			texture.SetData(data);

			return texture;
		}

		public static Texture2D CircFill(GraphicsDevice graphicsDevice, float radius, Color color)
		{
			int diameter = (int)(radius * 2);
			int textureSize = diameter + 1;
			Texture2D texture = new(graphicsDevice, textureSize, textureSize);

			Color[] data = new Color[textureSize * textureSize];

			int x = (int)radius;
			int y = 0;
			int radiusError = 1 - x;

			while (x >= y)
			{
				FillLine(data, textureSize, (int)(radius - x), (int)(radius + y), (int)(radius - y), color);
				FillLine(data, textureSize, (int)(radius - y), (int)(radius + x), (int)(radius - x), color);
				FillLine(data, textureSize, (int)(radius + x), (int)(radius + y), (int)(radius - y), color);
				FillLine(data, textureSize, (int)(radius + y), (int)(radius + x), (int)(radius - x), color);

				y++;

				if (radiusError < 0)
				{
					radiusError += (2 * y) + 1;
				}
				else
				{
					x--;
					radiusError += (2 * (y - x)) + 1;
				}
			}

			texture.SetData(data);
			return texture;
		}

		private static void FillLine(Color[] data, int textureSize, int x1, int y1, int y2, Color color)
		{
			if (y1 > y2)
			{
				(y2, y1) = (y1, y2);
			}

			for (int y = y1; y <= y2; y++)
			{
				if (x1 >= 0 && x1 < textureSize && y >= 0 && y < textureSize)
				{
					data[(y * textureSize) + x1] = color;
				}
			}
		}
	}
}