using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class InvulnerableSystem(World world) : SystemBase<GameTime>(world)
	{
		private GameTime _gameTime;
		private readonly QueryDescription _invulnerableEntities =
			new QueryDescription().WithAll<Invulnerable>();

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _invulnerableEntities,
				(Entity entity, ref Invulnerable invulnerable) =>
				{
					invulnerable.Duration -= (float)_gameTime.ElapsedGameTime.TotalSeconds;

					if (invulnerable.Duration <= 0)
					{
						World.Remove<Invulnerable>(entity);
					}
				}
			);
		}
	}
}
