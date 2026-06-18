using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Handles the player taking damage from an enemy or an enemy projectile. Loses a
	// life, plays the death SFX, and either flags game-over (at 0 lives) or respawns
	// the player with a ~1000ms invulnerability window (the opacity blink is driven
	// by InvulnerableSystem). Colliding enemy bullets are destroyed; enemies are left
	// to keep flying. CollisionSystem only emits this event when the player is NOT
	// already invulnerable, so the skip-while-invulnerable rule is enforced upstream.
	public class PlayerEnemyCollisionEventSystem(World world, State state, Config config)
		: SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private readonly Config _config = config;

		private const float InvulnerableSeconds = 1f;

		private readonly QueryDescription _eventQuery =
			new QueryDescription().WithAll<EventPlayerEnemyCollision>();

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _eventQuery,
				(Entity eventEntity, ref EventPlayerEnemyCollision collision) =>
				{
					var player = collision.PlayerEntity;
					var other = collision.EnemyEntity;

					// Destroy the colliding enemy bullet (enemies keep flying).
					if (World.IsAlive(other) && World.Has<TagEnemyBullet>(other))
					{
						World.Destroy(other);
					}

					if (!World.IsAlive(player))
					{
						World.Destroy(eventEntity);

						return;
					}

					SoundSystem.Play(World, "player-death");

					_state.Lives--;

					if (_state.Lives <= 0)
					{
						_state.GameOver = true;

						// Future GameOver screen (M5) will consume this; nothing does yet.
						World.Create(new EventGameOver());

						World.Destroy(player);
					}
					else
					{
						// Respawn at the configured spawn position and grant a brief
						// invulnerability window with an opacity blink.
						var transform = World.Get<Transform>(player);
						transform.Position = _config.Entities.Player.SpawnPosition;

						if (!World.Has<Invulnerable>(player))
						{
							World.Add(
								player,
								new Invulnerable() { Duration = InvulnerableSeconds }
							);
						}
					}

					World.Destroy(eventEntity);
				}
			);
		}
	}
}
