using System;
using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using CherryBomb.Lib;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	// Handles the player taking damage from an enemy or an enemy projectile. Loses a
	// life, plays the death SFX, spawns the blue player-explosion particle burst, and
	// either flags game-over (at 0 lives, with a 1s delay so the explosion plays out)
	// or respawns the player with a 2000ms invulnerability window (the opacity blink is
	// driven by InvulnerableSystem). Colliding enemy bullets are destroyed; enemies are
	// left to keep flying. CollisionSystem only emits this event when the player is NOT
	// already invulnerable, so the skip-while-invulnerable rule is enforced upstream.
	public class PlayerEnemyCollisionEventSystem(
		World world,
		State state,
		Config config,
		Scheduler scheduler
	) : SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private readonly Config _config = config;
		private readonly Scheduler _scheduler = scheduler;
		private readonly Random _random = new();

		// Source: player-enemy-collision-event-system.ts respawn window (2000ms).
		private const float InvulnerableSeconds = 2f;

		private readonly QueryDescription _eventQuery =
			new QueryDescription().WithAll<EventPlayerEnemyCollision>();

		private readonly QueryDescription _thrusterQuery = new QueryDescription().WithAll<
			Parent,
			Transform
		>();

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

					// Blue player-explosion particle burst, centred on the player.
					// Source: content.ts generatePlayerExplosionSpriteSheet (isBlue
					// particles + white shockwave). The C# port renders the particles
					// live rather than baking them into a spritesheet.
					var playerTransform = World.Get<Transform>(player);
					var playerSprite = World.Get<Sprite>(player);
					var center = new Vector2(
						playerTransform.Position.X + (playerSprite.CurrentFrame.Width / 2f),
						playerTransform.Position.Y + (playerSprite.CurrentFrame.Height / 2f)
					);

					SpawnPlayerExplosion(center);

					// Camera shake on every hit (mirrors spread-shot's shake, longer
					// and stronger). Consumed by CameraShakeSystem. Source: M5 spec.
					World.Create(new EventTriggerCameraShake(strength: 6, durationMs: 400));

					if (_state.Lives <= 0)
					{
						// Stop spawning new waves immediately, but let the death
						// explosion play out before swapping to the game-over screen.
						// Source: player-enemy-collision-event-system.ts (1000ms timer).
						_state.GameOver = true;

						World.Destroy(player);

						// Destroy the player's thruster child too.
						DestroyThruster(player);

						_scheduler.Add(1000f, () => World.Create(new EventGameOver()));
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

						// Move the thruster back to the spawn position too.
						var thruster = FindThruster(player);

						if (thruster != player && World.IsAlive(thruster))
						{
							ref var thrusterTransform = ref World.Get<Transform>(thruster);
							thrusterTransform.Position = _config.Entities.Player.SpawnPosition;
						}
					}

					World.Destroy(eventEntity);
				}
			);
		}

		// Locates the player's thruster child (Parent.Entity == player). Returns the
		// player itself if none is found (matches PlayerSystem.FindThruster).
		private Entity FindThruster(Entity player)
		{
			var thruster = player;

			World.Query(
				in _thrusterQuery,
				(Entity entity, ref Parent parent) =>
				{
					if (parent.Entity == player)
					{
						thruster = entity;
					}
				}
			);

			return thruster;
		}

		private void DestroyThruster(Entity player)
		{
			var thruster = FindThruster(player);

			if (thruster != player && World.IsAlive(thruster))
			{
				World.Destroy(thruster);
			}
		}

		// Mirrors the enemy-death burst but with isBlue particles, matching the
		// player-explosions generator: a white shockwave, an initial flash, a ring of
		// large blue particles, and faster blue sparks.
		private void SpawnPlayerExplosion(Vector2 center)
		{
			var white = new XnaColor(
				Pico8Color.Color7.R,
				Pico8Color.Color7.G,
				Pico8Color.Color7.B,
				Pico8Color.Color7.A
			);

			// Shockwave (radius 3 -> 25, Color7, speed 105).
			World.Create(
				new Shockwave(radius: 3, targetRadius: 25, color: Pico8Color.Color7, speed: 105),
				new Transform(position: center, rotation: 0f, scale: Vector2.One)
			);

			// Initial flash.
			ExplosionFactory.CreateExplosion(
				World,
				count: 1,
				() =>
					new Direction(
						x: 1 * Math.Sign((_random.NextSingle() * 2) - 1),
						y: 1 * Math.Sign((_random.NextSingle() * 2) - 1)
					),
				() =>
					new Particle(
						age: 0,
						maxAge: 0,
						color: white,
						isBlue: true,
						radius: 10,
						shape: Components.Shape.Circle,
						spark: false
					),
				() => center,
				() => new Velocity()
			);

			// Large blue particles (TS: count 30, maxAge 10 + rnd(20), vel up to 140).
			ExplosionFactory.CreateExplosion(
				World,
				count: 30,
				() =>
					new Direction(
						x: 1 * Math.Sign((_random.NextSingle() * 2) - 1),
						y: 1 * Math.Sign((_random.NextSingle() * 2) - 1)
					),
				() =>
					new Particle(
						age: _random.NextSingle(0.06f),
						maxAge: 0.266f + _random.NextSingle(0.533f),
						color: white,
						isBlue: true,
						radius: 1 + _random.NextSingle(4),
						shape: Components.Shape.Circle,
						spark: false
					),
				() => center,
				() => new Velocity(_random.NextSingle() * 90, _random.NextSingle() * 90)
			);

			// Blue sparks (TS: count 20, vel up to 300).
			ExplosionFactory.CreateExplosion(
				World,
				count: 20,
				() =>
					new Direction(
						x: 1 * Math.Sign((_random.NextSingle() * 2) - 1),
						y: 1 * Math.Sign((_random.NextSingle() * 2) - 1)
					),
				() =>
					new Particle(
						age: _random.NextSingle(0.06f),
						maxAge: 0.266f + _random.NextSingle(0.266f),
						color: white,
						isBlue: true,
						radius: 1 + _random.NextSingle(4),
						shape: Components.Shape.Circle,
						spark: true
					),
				() => center,
				() => new Velocity(_random.NextSingle() * 150, _random.NextSingle() * 150)
			);
		}
	}
}
