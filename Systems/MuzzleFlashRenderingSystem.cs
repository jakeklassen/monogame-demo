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
	// Draws each muzzle flash as a small filled white circle (Color7) centered on
	// its Transform, using the pre-generated circfill texture cache. Source: muzzle-flash-rendering-system.ts.
	public class MuzzleFlashRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera,
		Dictionary<string, Texture2D> textureCache
	) : SystemBase<GameTime>(world)
	{
		private readonly SpriteBatch _spriteBatch = spriteBatch;
		private readonly OrthographicCamera _camera = camera;
		private readonly Dictionary<string, Texture2D> _textureCache = textureCache;
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			MuzzleFlash,
			Transform
		>();

		private static readonly XnaColor FlashColor = new(
			Pico8Color.Color7.R,
			Pico8Color.Color7.G,
			Pico8Color.Color7.B,
			Pico8Color.Color7.A
		);

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
				(Entity entity, ref Transform transform) =>
				{
					var muzzleFlash = World.Get<MuzzleFlash>(entity);

					var radius = (int)MathF.Round(muzzleFlash.Size);

					if (radius < 1)
					{
						return;
					}

					radius = Math.Clamp(radius, 1, 32);

					var texture = _textureCache[$"circfill-{radius}"];

					_spriteBatch.Draw(
						texture,
						transform.Position - new Vector2(radius, radius),
						FlashColor
					);
				}
			);

			_spriteBatch.End();
		}
	}
}
