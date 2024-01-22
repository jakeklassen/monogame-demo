using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb;
using Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Systems
{
	public class ParticleRenderingSystem(
		World world,
		GraphicsDevice graphicsDevice,
		OrthographicCamera camera,
		Dictionary<string, Texture2D> textureCache
		) : SystemBase<GameTime>(world)
	{
		private readonly SpriteBatch _spriteBatch = new(graphicsDevice);
		private readonly OrthographicCamera _camera = camera;
		private readonly Dictionary<string, Texture2D> _textureCache = textureCache;
		private QueryDescription _query = new QueryDescription().WithAll<Particle, Transform>();

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
				(Entity entity, ref Particle particle, ref Transform transform) =>
				{
					if (Math.Floor(particle.Radius) <= 0)
					{
						return;
					}

					if (particle.Spark)
					{
						_spriteBatch.DrawRectangle(
							new RectangleF(transform.Position.X, transform.Position.Y, 1, 1),
							new Microsoft.Xna.Framework.Color(
								Pico8Color.Color7.R,
								Pico8Color.Color7.G,
								Pico8Color.Color7.B,
								Pico8Color.Color7.A
							)
						);
					}
					else if (particle.Shape == Components.Shape.Circle)
					{
						var texture = _textureCache[$"circfill-{Math.Floor(particle.Radius)}"];

						_spriteBatch.Draw(
							texture,
							transform.Position
								- new Vector2(MathF.Floor(particle.Radius), MathF.Floor(particle.Radius)),
							particle.Color
						);
					}
				}
			);

			_spriteBatch.End();
		}
	}
}
