using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class ShockwaveSystem : SystemBase<GameTime>
	{
		private GameTime _gameTime;
		private QueryDescription _query = new QueryDescription().WithAll<Shockwave>();

		public ShockwaveSystem(World world)
			: base(world) { }

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _query,
				(in Entity entity, ref Shockwave shockwave) =>
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
