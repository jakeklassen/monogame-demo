using Arch.Core;
using CherryBomb;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class ParticleSystem : SystemBase<GameTime>
	{
		private GameTime _gameTime;
		private QueryDescription _particleEntities = new QueryDescription().WithAll<
			Particle,
			Velocity
		>();

		public ParticleSystem(World world)
			: base(world) { }

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _particleEntities,
				(in Entity entity, ref Particle particle, ref Velocity velocity) =>
				{
					particle.Age += (float)_gameTime.ElapsedGameTime.TotalSeconds;
					velocity.X -= velocity.X * 0.85f * (float)_gameTime.ElapsedGameTime.TotalSeconds;
					velocity.Y -= velocity.Y * 0.85f * (float)_gameTime.ElapsedGameTime.TotalSeconds;

					if (particle.Age >= particle.MaxAge)
					{
						particle.Radius -= 0.5f;

						if (particle.Radius <= 0)
						{
							World.Destroy(entity);

							return;
						}
					}

					particle.Color = particle.IsBlue
						? DetermineParticleColorFromAge(particle, "blue")
						: DetermineParticleColorFromAge(particle, "red");
				}
			);
		}

		private static Microsoft.Xna.Framework.Color DetermineParticleColorFromAge(
			Particle particle,
			string bias
		)
		{
			if (bias == "red")
			{
				if (particle.Age > 0.5)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color5.R,
						Pico8Color.Color5.G,
						Pico8Color.Color5.B,
						Pico8Color.Color5.A
					);
				}

				if (particle.Age > 0.4)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color2.R,
						Pico8Color.Color2.G,
						Pico8Color.Color2.B,
						Pico8Color.Color2.A
					);
				}

				if (particle.Age > 0.33)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color8.R,
						Pico8Color.Color8.G,
						Pico8Color.Color8.B,
						Pico8Color.Color8.A
					);
				}

				if (particle.Age > 0.233)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color9.R,
						Pico8Color.Color9.G,
						Pico8Color.Color9.B,
						Pico8Color.Color9.A
					);
				}

				if (particle.Age > 0.166)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color10.R,
						Pico8Color.Color10.G,
						Pico8Color.Color10.B,
						Pico8Color.Color10.A
					);
				}
			}
			else if (bias == "blue")
			{
				if (particle.Age > 0.5)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color1.R,
						Pico8Color.Color1.G,
						Pico8Color.Color1.B,
						Pico8Color.Color1.A
					);
				}

				if (particle.Age > 0.4)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color1.R,
						Pico8Color.Color1.G,
						Pico8Color.Color1.B,
						Pico8Color.Color1.A
					);
				}

				if (particle.Age > 0.33)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color13.R,
						Pico8Color.Color13.G,
						Pico8Color.Color13.B,
						Pico8Color.Color13.A
					);
				}

				if (particle.Age > 0.233)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color12.R,
						Pico8Color.Color12.G,
						Pico8Color.Color12.B,
						Pico8Color.Color12.A
					);
				}

				if (particle.Age > 0.166)
				{
					return new Microsoft.Xna.Framework.Color(
						Pico8Color.Color6.R,
						Pico8Color.Color6.G,
						Pico8Color.Color6.B,
						Pico8Color.Color6.A
					);
				}
			}

			return new Microsoft.Xna.Framework.Color(
				Pico8Color.Color7.R,
				Pico8Color.Color7.G,
				Pico8Color.Color7.B,
				Pico8Color.Color7.A
			);
		}
	}
}
