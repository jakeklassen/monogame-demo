using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	public class InvulnerableSystem(World world) : SystemBase<GameTime>(world)
	{
		private GameTime _gameTime;

		// Blink toggle period in seconds: sprite opacity flips fully on/off this often
		// while invulnerable, giving the classic post-hit flicker.
		private const float BlinkPeriod = 0.1f;

		private readonly QueryDescription _invulnerableEntities =
			new QueryDescription().WithAll<Invulnerable>();

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _invulnerableEntities,
				(Entity entity, ref Invulnerable invulnerable) =>
				{
					var dt = (float)_gameTime.ElapsedGameTime.TotalSeconds;

					invulnerable.Duration -= dt;
					invulnerable.ElapsedSeconds += dt;

					// Opacity-blink: only the player flickers after a hit. Enemies are
					// also granted invulnerability during fly-in, so we deliberately
					// leave their opacity alone.
					var blinks = World.Has<TagPlayer>(entity) && World.Has<Sprite>(entity);

					if (blinks)
					{
						ref var sprite = ref World.Get<Sprite>(entity);

						var blinkOn = (int)(invulnerable.ElapsedSeconds / BlinkPeriod) % 2 == 0;

						sprite.Opacity = blinkOn ? 0.4f : 1f;
					}

					if (invulnerable.Duration <= 0)
					{
						// Restore full opacity before dropping the component.
						if (blinks)
						{
							ref var sprite = ref World.Get<Sprite>(entity);
							sprite.Opacity = 1f;
						}

						World.Remove<Invulnerable>(entity);
					}
				}
			);
		}
	}
}
