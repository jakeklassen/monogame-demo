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
	public class ShockwaveRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera,
		Dictionary<string, Texture2D> textureCache
	) : SystemBase<GameTime>(world)
	{
		private readonly SpriteBatch _spriteBatch = spriteBatch;
		private readonly OrthographicCamera _camera = camera;
		private readonly Dictionary<string, Texture2D> _textureCache = textureCache;
		private QueryDescription _query = new QueryDescription().WithAll<Shockwave, Transform>();

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
				(Entity entity, ref Shockwave shockwave, ref Transform transform) =>
				{
					var texture = _textureCache[$"circ-{Math.Round(shockwave.Radius)}"];
					var shockwaveColor = new XnaColor(
						shockwave.Color.R,
						shockwave.Color.G,
						shockwave.Color.B,
						shockwave.Color.A
					);

					_spriteBatch.Draw(
						texture,
						transform.Position
							- new Vector2(
								MathF.Round(shockwave.Radius),
								MathF.Round(shockwave.Radius)
							),
						shockwaveColor
					);
				}
			);

			_spriteBatch.End();
		}
	}
}
