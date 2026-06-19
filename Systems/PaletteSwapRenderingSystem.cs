using System.Collections.Generic;
using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	// Draws sprites tagged with PaletteSwap using a recolored copy of their
	// current frame (Color3 -> Color8, Color11 -> Color14), faithfully matching
	// the boss hurt palette swap in the TS source instead of a solid silhouette.
	// Recolored frames are baked once and cached (no per-frame pixel work).
	public class PaletteSwapRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera,
		Texture2D spriteSheetTexture
	) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			PaletteSwap,
			Sprite,
			Transform
		>();
		private readonly SpriteBatch _spriteBatch = spriteBatch;
		private readonly OrthographicCamera _camera = camera;
		private readonly Texture2D _spriteSheetTexture = spriteSheetTexture;
		private readonly Dictionary<Rectangle, Texture2D> _recolored = [];

		public override void Update(in GameTime gameTime)
		{
			_spriteBatch.Begin(
				SpriteSortMode.Deferred,
				null,
				SamplerState.PointClamp,
				null,
				null,
				null,
				_camera.GetViewMatrix()
			);

			World.Query(
				in _query,
				(Entity entity, ref Sprite sprite, ref Transform transform) =>
				{
					var texture = GetRecolored(sprite.CurrentFrame);
					_spriteBatch.Draw(texture, transform.Position, XnaColor.White);
				}
			);

			_spriteBatch.End();
		}

		// Bakes a recolored copy of a source frame: pixels matching Color3 become
		// Color8 and pixels matching Color11 become Color14; every other pixel
		// (eye, outline, transparency) is preserved. Cached per frame rectangle.
		private Texture2D GetRecolored(Rectangle frame)
		{
			if (_recolored.TryGetValue(frame, out var cached))
			{
				return cached;
			}

			var pixels = new XnaColor[frame.Width * frame.Height];
			_spriteSheetTexture.GetData(0, frame, pixels, 0, pixels.Length);

			var from1 = Pico8Color.Color3.XnaColor;
			var to1 = Pico8Color.Color8.XnaColor;
			var from2 = Pico8Color.Color11.XnaColor;
			var to2 = Pico8Color.Color14.XnaColor;

			for (var i = 0; i < pixels.Length; i++)
			{
				var p = pixels[i];

				if (p.A == 0)
				{
					continue;
				}

				if (p.R == from1.R && p.G == from1.G && p.B == from1.B)
				{
					pixels[i] = new XnaColor(to1.R, to1.G, to1.B, p.A);
				}
				else if (p.R == from2.R && p.G == from2.G && p.B == from2.B)
				{
					pixels[i] = new XnaColor(to2.R, to2.G, to2.B, p.A);
				}
			}

			var texture = new Texture2D(_spriteBatch.GraphicsDevice, frame.Width, frame.Height);
			texture.SetData(pixels);
			_recolored.Add(frame, texture);

			return texture;
		}
	}
}
