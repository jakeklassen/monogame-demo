using System;
using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using CherryBomb.Lib;
using Microsoft.Xna.Framework;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	// Scripted boss death / game-won sequence. Triggered by EventDestroyBoss:
	//   t=0      stop the boss, lock it into the hurt frame, drop its collider,
	//            disable it, request a long camera shake (4000ms, str 3), and
	//            start repeating small explosions every 100ms (enemy-death SFX).
	//   t=4100ms floating "10000" score text, big-explosion SFX, a strong shake
	//            (250ms str 6), a big white shockwave + flash, white + blue spark
	//            bursts, then delete the boss and add 10000 to the score.
	//   t=7000ms emit EventGameWon and stop gameplay (the GameWon screen is M5).
	//
	// Deviation: the TS plays sprite-sheet explosion animations; this port has no
	// explosion atlas, so the repeating "small explosions" are particle bursts
	// (matching the existing particle-based enemy death) instead of sprites.
	// Source: systems/destroy-boss-event-system.ts.
	public class DestroyBossEventSystem(
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

		private readonly QueryDescription _eventQuery =
			new QueryDescription().WithAll<EventDestroyBoss>();

		private readonly QueryDescription _bossQuery = new QueryDescription().WithAll<
			TagBoss,
			Direction,
			EnemyType,
			Sprite,
			Transform
		>();

		// Latches so the (one-shot) sequence is only armed once even if more than
		// one EventDestroyBoss slips through on the trigger frame.
		private bool _triggered;

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _eventQuery,
				(Entity entity) =>
				{
					World.Destroy(entity);

					if (_triggered)
					{
						return;
					}

					Entity boss = default;
					var found = false;

					World.Query(
						in _bossQuery,
						(Entity bossEntity) =>
						{
							if (!found)
							{
								boss = bossEntity;
								found = true;
							}
						}
					);

					if (!found)
					{
						return;
					}

					_triggered = true;

					StartSequence(boss);
				}
			);
		}

		private void StartSequence(Entity boss)
		{
			// Stop boss movement.
			ref var direction = ref World.Get<Direction>(boss);
			direction.X = 0;
			direction.Y = 0;

			var bossConfig = _config.Entities.Enemies.GetEnemyConfig(
				World.Get<EnemyType>(boss).Value
			);
			var bonusScore = bossConfig?.Score ?? 10_000;

			// Lock the boss into the hurt frame.
			ref var sprite = ref World.Get<Sprite>(boss);
			sprite.CurrentFrame = SpriteSheet.Enemies.Boss.HurtFrame;

			if (World.Has<SpriteAnimation>(boss))
			{
				var hurt = SpriteSheet.Enemies.Boss.Animations["Hurt"];

				World.Set(
					boss,
					SpriteAnimation.Factory(
						animationDetails: new AnimationDetails()
						{
							Name = "boss-hurt",
							SourceX = hurt.SourceX,
							SourceY = hurt.SourceY,
							Width = hurt.Width,
							Height = hurt.Height,
							FrameWidth = hurt.FrameWidth,
							FrameHeight = hurt.FrameHeight,
						},
						durationSeconds: 0.1f,
						loop: true
					)
				);
			}

			// Disable collisions (can't keep shooting / flying into it) and mark it
			// disabled so BossSystem stops driving it.
			if (World.Has<BoxCollider>(boss))
			{
				World.Remove<BoxCollider>(boss);
			}

			if (!World.Has<TagDisabled>(boss))
			{
				World.Add(boss, new TagDisabled());
			}

			// Long camera shake (consumer arrives in M5).
			World.Create(new EventTriggerCameraShake(strength: 3, durationMs: 4000));

			// Repeating small explosions every 100ms until the big blast at 4100ms.
			var explosionTimer = _scheduler.AddRepeating(
				100f,
				() =>
				{
					if (!World.IsAlive(boss))
					{
						return;
					}

					SoundSystem.Play(World, "enemy-death");

					var bossPos = World.Get<Transform>(boss).Position;
					var bossFrame = SpriteSheet.Enemies.Boss.Frame;

					SpawnSmallExplosion(
						new Vector2(
							bossPos.X + _random.Next(bossFrame.Width) - 32,
							bossPos.Y + _random.Next(bossFrame.Height) - 32
						)
					);
				}
			);

			// The big blast at 4100ms.
			_scheduler.Add(
				4100f,
				() =>
				{
					_scheduler.Remove(explosionTimer);

					if (!World.IsAlive(boss))
					{
						return;
					}

					var bossPos = World.Get<Transform>(boss).Position;
					var bossFrame = SpriteSheet.Enemies.Boss.Frame;
					var center = new Vector2(
						bossPos.X + (bossFrame.Width / 2f),
						bossPos.Y + (bossFrame.Height / 2f)
					);

					SpawnBonusText(bonusScore, new Vector2(bossPos.X + 16, bossPos.Y + 12));

					SoundSystem.Play(World, "big-explosion");

					// Slightly stronger shake.
					World.Create(new EventTriggerCameraShake(strength: 6, durationMs: 250));

					// Big white shockwave.
					World.Create(
						new Shockwave(
							radius: 3,
							targetRadius: 25,
							color: Pico8Color.Color7,
							speed: 105
						),
						new Transform(position: center, rotation: 0f, scale: Vector2.One)
					);

					// Initial white flash particle.
					SpawnFlash(center);

					// 60 white particles.
					SpawnParticleBurst(center, count: 60, isBlue: false, spark: false, 200f, 6f);

					// 100 blue spark particles.
					SpawnParticleBurst(center, count: 100, isBlue: true, spark: true, 350f, 4f);

					World.Destroy(boss);

					_state.Score += bonusScore;
				}
			);

			// Game won at 7000ms.
			_scheduler.Add(
				7000f,
				() =>
				{
					_scheduler.Remove(explosionTimer);

					_state.GameOver = true;

					World.Create(new EventGameWon());
				}
			);
		}

		private void SpawnSmallExplosion(Vector2 position)
		{
			ExplosionFactory.CreateExplosion(
				World,
				count: 8,
				() => Direction.Random(),
				() =>
					new Particle(
						age: _random.NextSingle() * 0.06f,
						maxAge: 0.2f + (_random.NextSingle() * 0.2f),
						color: ToXna(Pico8Color.Color7),
						isBlue: false,
						radius: 1 + (_random.NextSingle() * 4),
						shape: Shape.Circle,
						spark: false
					),
				() => position,
				() => new Velocity(_random.NextSingle() * 80, _random.NextSingle() * 80)
			);
		}

		private void SpawnFlash(Vector2 position)
		{
			ExplosionFactory.CreateExplosion(
				World,
				count: 1,
				() => Direction.Random(),
				() =>
					new Particle(
						age: 0,
						maxAge: 0,
						color: ToXna(Pico8Color.Color7),
						isBlue: false,
						radius: 25,
						shape: Shape.Circle,
						spark: false
					),
				() => position,
				() => new Velocity()
			);
		}

		private void SpawnParticleBurst(
			Vector2 position,
			int count,
			bool isBlue,
			bool spark,
			float maxVelocity,
			float maxRadius
		)
		{
			ExplosionFactory.CreateExplosion(
				World,
				count: count,
				() => Direction.Random(),
				() =>
					new Particle(
						age: _random.NextSingle() * 0.06f,
						maxAge: 0.2f + (_random.NextSingle() * 0.2f),
						color: ToXna(Pico8Color.Color7),
						isBlue: isBlue,
						radius: 1 + (_random.NextSingle() * maxRadius),
						shape: Shape.Circle,
						spark: spark
					),
				() => position,
				() =>
					new Velocity(
						_random.NextSingle() * maxVelocity,
						_random.NextSingle() * maxVelocity
					)
			);
		}

		private void SpawnBonusText(int bonusScore, Vector2 position)
		{
			var textEntity = this.World.Create();

			this.World.Add(textEntity, new Direction(0, -1));
			this.World.Add(
				textEntity,
				new Text()
				{
					Alignment = Alignment.Center,
					Color = Pico8Color.Color7,
					Content = $"{bonusScore}",
					Font = "pico-8",
				}
			);
			this.World.Add(
				textEntity,
				new Blink(
					colors: [Pico8Color.Color7, Pico8Color.Color8],
					colorSequence: [0, 1],
					durationSeconds: 0.1f
				)
			);
			this.World.Add(textEntity, new Transform(position, 0f, Vector2.One));
			this.World.Add(textEntity, new TimeToLive(2f));
			this.World.Add(textEntity, new Velocity(0, 15));
		}

		private static XnaColor ToXna(Components.Color color) =>
			new(color.R, color.G, color.B, color.A);
	}
}
