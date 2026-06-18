using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	// Draws a 1px dilation outline behind each outlined sprite. The outline texture
	// (a colored silhouette dilated by one pixel, with the sprite interior left
	// transparent) is pre-baked once per (source frame, color) pair and cached for
	// the screen's lifetime — no per-pixel work happens each frame. This reuses the
	// cached-mask technique from FlashRenderingSystem. Source: sprite-outline-rendering-system.ts.
	public class SpriteOutlineRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera,
		Texture2D spriteSheetTexture
	) : SystemBase<GameTime>(world)
	{
		private readonly SpriteBatch _spriteBatch = spriteBatch;
		private readonly OrthographicCamera _camera = camera;
		private readonly Texture2D _spriteSheetTexture = spriteSheetTexture;

		private const int Thickness = 1;

		private readonly QueryDescription _query = new QueryDescription().WithAll<
			Sprite,
			SpriteOutline,
			Transform
		>();

		// Cache keyed by (source frame, packed outline color).
		private readonly Dictionary<(Rectangle, uint), Texture2D> _outlines = [];

		public override void Update(in GameTime gameTime)
		{
			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				null,
				SamplerState.PointClamp,
				null,
				null,
				null,
				_camera.GetViewMatrix()
			);

			World.Query(
				in _query,
				(
					Entity entity,
					ref Sprite sprite,
					ref SpriteOutline outline,
					ref Transform transform
				) =>
				{
					var color = new XnaColor(
						outline.Color.R,
						outline.Color.G,
						outline.Color.B,
						outline.Color.A
					);

					var texture = GetOutline(sprite.CurrentFrame, color);

					// Offset by the outline thickness so the dilated ring sits flush
					// around the sprite (which is drawn at transform.Position).
					_spriteBatch.Draw(
						texture,
						transform.Position - new Vector2(Thickness, Thickness),
						null,
						XnaColor.White * sprite.Opacity,
						transform.Rotation,
						Vector2.Zero,
						1f,
						SpriteEffects.None,
						0f
					);
				}
			);

			_spriteBatch.End();
		}

		// Builds (once per frame+color) a dilated outline: start from the sprite's
		// silhouette padded by Thickness on every side, then paint the outline color
		// into every transparent pixel that neighbors a non-transparent source pixel.
		// The sprite interior is left transparent so the real sprite shows through.
		private Texture2D GetOutline(Rectangle frame, XnaColor color)
		{
			var key = (frame, color.PackedValue);

			if (_outlines.TryGetValue(key, out var cached))
			{
				return cached;
			}

			var srcWidth = frame.Width;
			var srcHeight = frame.Height;
			var width = srcWidth + (Thickness * 2);
			var height = srcHeight + (Thickness * 2);

			var src = new XnaColor[srcWidth * srcHeight];
			_spriteSheetTexture.GetData(0, frame, src, 0, src.Length);

			// Padded silhouette grid: true where the source pixel is opaque.
			var solid = new bool[width * height];

			for (var sy = 0; sy < srcHeight; sy++)
			{
				for (var sx = 0; sx < srcWidth; sx++)
				{
					if (src[(sy * srcWidth) + sx].A > 0)
					{
						solid[((sy + Thickness) * width) + (sx + Thickness)] = true;
					}
				}
			}

			var pixels = new XnaColor[width * height];

			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var index = (y * width) + x;

					// Interior pixels stay transparent (the sprite is drawn on top).
					if (solid[index])
					{
						continue;
					}

					// A transparent pixel becomes outline if any neighbor is solid.
					var isOutline = false;

					for (var dy = -Thickness; dy <= Thickness && !isOutline; dy++)
					{
						for (var dx = -Thickness; dx <= Thickness; dx++)
						{
							var nx = x + dx;
							var ny = y + dy;

							if (nx < 0 || nx >= width || ny < 0 || ny >= height)
							{
								continue;
							}

							if (solid[(ny * width) + nx])
							{
								isOutline = true;

								break;
							}
						}
					}

					if (isOutline)
					{
						pixels[index] = color;
					}
				}
			}

			var texture = new Texture2D(_spriteBatch.GraphicsDevice, width, height);
			texture.SetData(pixels);
			_outlines.Add(key, texture);

			return texture;
		}
	}
}
