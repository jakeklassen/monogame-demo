using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class ShockwaveSystem(World world) : SystemBase<GameTime>(world)
	{
		private GameTime _gameTime;
		private QueryDescription _query = new QueryDescription().WithAll<Shockwave>();

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _query,
				(Entity entity, ref Shockwave shockwave) =>
				{
					shockwave.Radius += shockwave.Speed * (float)_gameTime.ElapsedGameTime.TotalSeconds;

					if (shockwave.Radius >= shockwave.TargetRadius)
					{
						World.Destroy(entity);
					}
				}
			);
		}
	}
}
