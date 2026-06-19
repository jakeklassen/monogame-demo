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
	public class ParticleRenderingSystem : SystemBase<GameTime>
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly OrthographicCamera _camera;
		private QueryDescription _query = new QueryDescription().WithAll<Particle, Transform>();

		// Filled-circle textures indexed directly by integer radius (1..MaxRadius).
		// Built once so the hot draw loop never allocates a "circfill-{r}" string per
		// particle per frame (which was ~60k string allocations/sec during a bomb).
		private readonly Texture2D[] _circFill;

		// Sparks are always this color; build it once instead of per spark per frame.
		private static readonly XnaColor SparkColor = new(
			Pico8Color.Color7.R,
			Pico8Color.Color7.G,
			Pico8Color.Color7.B,
			Pico8Color.Color7.A
		);

		public ParticleRenderingSystem(
			World world,
			SpriteBatch spriteBatch,
			OrthographicCamera camera,
			Dictionary<string, Texture2D> textureCache
		)
			: base(world)
		{
			_spriteBatch = spriteBatch;
			_camera = camera;

			var max = 32;
			_circFill = new Texture2D[max + 1];
			for (var r = 1; r <= max; r++)
			{
				_circFill[r] = textureCache[$"circfill-{r}"];
			}
		}

		public override void Update(in GameTime gameTime)
		{
			// Deferred batches all particle draws into a few GPU calls. Immediate
			// flushed one draw per particle (~1000+ calls during a bomb).
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
				(Entity entity, ref Particle particle, ref Transform transform) =>
				{
					var r = (int)particle.Radius;

					if (r <= 0)
					{
						return;
					}

					if (particle.Spark)
					{
						_spriteBatch.DrawRectangle(
							new RectangleF(transform.Position.X, transform.Position.Y, 1, 1),
							SparkColor
						);
					}
					else if (particle.Shape == Components.Shape.Circle && r < _circFill.Length)
					{
						_spriteBatch.Draw(
							_circFill[r],
							transform.Position - new Vector2(r, r),
							particle.Color
						);
					}
				}
			);

			_spriteBatch.End();
		}
	}
}
