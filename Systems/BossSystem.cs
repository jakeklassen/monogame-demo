using System;
using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Drives the wave-9 boss through a repeating 4-phase attack cycle once it
	// reaches formation (EnemyStateType.BossReady). The boss is clamped to
	// x in [3, 93] and y in [25, 100] and flips direction at the bounds.
	//
	// All phase durations and shot cadences are tracked with dt-driven
	// accumulators (seconds) rather than the Scheduler, so a phase change cleanly
	// abandons its in-flight timers without churning scheduler callbacks. Bullets
	// are fired through the EnemyBullet helpers (speed 60) with the boss-projectile
	// SFX. Source: systems/boss-system.ts.
	public class BossSystem(World world) : SystemBase<GameTime>(world)
	{
		private const int NumberOfBullets = 8;
		private const float BulletSpeed = 60f;

		private const float MinX = 3f;
		private const float MaxX = 93f;
		private const float MinY = 25f;
		private const float MaxY = 100f;

		private readonly QueryDescription _bossQuery = new QueryDescription().WithAll<
			TagBoss,
			Direction,
			EnemyState,
			Transform,
			Velocity
		>();

		private readonly QueryDescription _playerQuery = new QueryDescription().WithAll<
			TagPlayer,
			Transform
		>();

		// 0 = not started; 1..4 = active phase.
		private int _phase;

		// Phase-1 / phase-3 elapse over 8s; the "no timer" phases (2 and 4) exit on
		// geometry instead.
		private float _phaseTimer;

		// Per-shot accumulators (seconds).
		private float _fireTimer;
		private int _shotsFired;

		// Running clock used to rotate the phase-3 radial ring (mirrors the TS
		// performance.now()/1000 spin factor).
		private float _clock;

		public override void Update(in GameTime gameTime)
		{
			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			_clock += dt;

			Entity boss = default;
			var found = false;

			World.Query(
				in _bossQuery,
				(Entity entity) =>
				{
					if (!found)
					{
						boss = entity;
						found = true;
					}
				}
			);

			if (!found || World.Has<TagDisabled>(boss))
			{
				_phase = 0;

				return;
			}

			var state = World.Get<EnemyState>(boss);

			// Not ready yet (still flying in): nothing to drive.
			if (state.Value != EnemyStateType.BossReady && _phase == 0)
			{
				return;
			}

			// First frame after BossReady: enter phase 1.
			if (state.Value == EnemyStateType.BossReady)
			{
				state.Value = EnemyStateType.Boss1;
				EnterPhase1(boss);
			}

			switch (_phase)
			{
				case 1:
					UpdatePhase1(boss, dt);

					break;
				case 2:
					UpdatePhase2(boss, dt);

					break;
				case 3:
					UpdatePhase3(boss, dt);

					break;
				case 4:
					UpdatePhase4(boss, dt);

					break;
			}
		}

		// ----- Phase 1: horizontal bounce + straight-down bursts ----------------
		private void EnterPhase1(Entity boss)
		{
			_phase = 1;
			_phaseTimer = 0f;
			_fireTimer = 0f;
			_shotsFired = 0;

			ref var direction = ref World.Get<Direction>(boss);
			ref var velocity = ref World.Get<Velocity>(boss);

			direction.X = Random.Shared.NextDouble() > 0.5 ? 1 : -1;
			direction.Y = 0;
			velocity.X = 60f;
			velocity.Y = 0f;
		}

		private void UpdatePhase1(Entity boss, float dt)
		{
			_phaseTimer += dt;

			if (_phaseTimer >= 8f)
			{
				EnterPhase2(boss);

				return;
			}

			// Straight-down burst on a 100ms timer: fire while shotsFired <= 8,
			// idle through the gap, then reset at >= 10 and repeat.
			_fireTimer += dt;

			if (_fireTimer >= 0.1f)
			{
				_fireTimer -= 0.1f;
				_shotsFired++;

				if (_shotsFired <= 8)
				{
					EnemyBulletFactory.Fire(World, boss, 0f, BulletSpeed, triggerSound: true);
				}
				else if (_shotsFired >= 10)
				{
					_shotsFired = 0;
				}
			}

			BounceHorizontally(boss);
		}

		// ----- Phase 2: perimeter traverse + aimed fire -------------------------
		private void EnterPhase2(Entity boss)
		{
			_phase = 2;
			_fireTimer = 0f;

			ref var direction = ref World.Get<Direction>(boss);
			ref var velocity = ref World.Get<Velocity>(boss);

			direction.X = -1;
			direction.Y = 0;
			velocity.X = 30f;
			velocity.Y = 30f;
		}

		private void UpdatePhase2(Entity boss, float dt)
		{
			_fireTimer += dt;

			ref var direction = ref World.Get<Direction>(boss);
			ref var transform = ref World.Get<Transform>(boss);

			if (direction.X == -1 && transform.Position.X <= MinX)
			{
				transform.Position = new Vector2(MinX, transform.Position.Y);
				direction.X = 0;
				direction.Y = 1;
			}
			else if (direction.Y == 1 && transform.Position.Y >= MaxY)
			{
				transform.Position = new Vector2(transform.Position.X, MaxY);
				direction.X = 1;
				direction.Y = 0;
			}
			else if (direction.X == 1 && transform.Position.X >= MaxX)
			{
				transform.Position = new Vector2(MaxX, transform.Position.Y);
				direction.X = 0;
				direction.Y = -1;
			}
			else if (direction.Y == -1 && transform.Position.Y <= MinY)
			{
				transform.Position = new Vector2(transform.Position.X, MinY);
				direction.X = 0;
				direction.Y = 0;

				EnterPhase3(boss);

				return;
			}

			AimedFireAtPlayer(boss, 0.5f);
		}

		// ----- Phase 3: horizontal bounce + rotating radial ring ----------------
		private void EnterPhase3(Entity boss)
		{
			_phase = 3;
			_phaseTimer = 0f;
			_fireTimer = 0f;

			ref var direction = ref World.Get<Direction>(boss);
			ref var velocity = ref World.Get<Velocity>(boss);

			direction.X = -1;
			direction.Y = 0;
			velocity.X = 30f;
			velocity.Y = 30f;
		}

		private void UpdatePhase3(Entity boss, float dt)
		{
			_phaseTimer += dt;
			_fireTimer += dt;

			if (_phaseTimer >= 8f)
			{
				EnterPhase4(boss);

				return;
			}

			if (_fireTimer >= 1f / 3f)
			{
				_fireTimer -= 1f / 3f;

				SoundSystem.Play(World, "boss-projectile");

				// Rotating ring of 8, spun by the running clock (radians).
				var angleStep = MathHelper.TwoPi / NumberOfBullets;
				var baseAngle = angleStep * _clock;

				for (var i = 0; i < NumberOfBullets; i++)
				{
					var circleAngle = baseAngle + (angleStep * i);
					var degrees = MathHelper.ToDegrees(circleAngle);

					EnemyBulletFactory.Fire(World, boss, degrees, BulletSpeed, triggerSound: false);
				}
			}

			BounceHorizontally(boss);
		}

		// ----- Phase 4: perimeter traverse + single directional bullet ----------
		private void EnterPhase4(Entity boss)
		{
			_phase = 4;
			_fireTimer = 0f;

			ref var direction = ref World.Get<Direction>(boss);
			ref var velocity = ref World.Get<Velocity>(boss);

			// Enter moving to the right.
			direction.X = 1;
			direction.Y = 0;
			velocity.X = 30f;
			velocity.Y = 30f;
		}

		private void UpdatePhase4(Entity boss, float dt)
		{
			_fireTimer += dt;

			ref var direction = ref World.Get<Direction>(boss);
			ref var transform = ref World.Get<Transform>(boss);

			if (direction.X == 1 && transform.Position.X >= MaxX)
			{
				transform.Position = new Vector2(MaxX, transform.Position.Y);
				direction.X = 0;
				direction.Y = 1;
			}
			else if (direction.Y == 1 && transform.Position.Y >= MaxY)
			{
				transform.Position = new Vector2(transform.Position.X, MaxY);
				direction.X = -1;
				direction.Y = 0;
			}
			else if (direction.X == -1 && transform.Position.X <= MinX)
			{
				transform.Position = new Vector2(MinX, transform.Position.Y);
				direction.X = 0;
				direction.Y = -1;
			}
			else if (direction.Y == -1 && transform.Position.Y <= MinY)
			{
				transform.Position = new Vector2(transform.Position.X, MinY);
				direction.Y = 0;
				direction.X = 1;

				EnterPhase1(boss);

				return;
			}

			if (_fireTimer >= 0.45f)
			{
				_fireTimer -= 0.45f;

				// Angle 0 down, 90 right, 180 up, 270 left — fire opposite the
				// current travel so shots trail the boss along the perimeter.
				if (direction.X > 0)
				{
					EnemyBulletFactory.Fire(World, boss, 0f, BulletSpeed, triggerSound: true);
				}
				else if (direction.X < 0)
				{
					EnemyBulletFactory.Fire(World, boss, 180f, BulletSpeed, triggerSound: true);
				}
				else if (direction.Y > 0)
				{
					EnemyBulletFactory.Fire(World, boss, 270f, BulletSpeed, triggerSound: true);
				}
				else if (direction.Y < 0)
				{
					EnemyBulletFactory.Fire(World, boss, 90f, BulletSpeed, triggerSound: true);
				}
			}
		}

		// ----- Helpers ----------------------------------------------------------
		private void BounceHorizontally(Entity boss)
		{
			ref var direction = ref World.Get<Direction>(boss);
			ref var transform = ref World.Get<Transform>(boss);

			if (transform.Position.X >= MaxX)
			{
				transform.Position = new Vector2(MaxX, transform.Position.Y);
				direction.X = -1;
			}
			else if (transform.Position.X <= MinX)
			{
				transform.Position = new Vector2(MinX, transform.Position.Y);
				direction.X = 1;
			}
		}

		private void AimedFireAtPlayer(Entity boss, float intervalSeconds)
		{
			if (_fireTimer < intervalSeconds)
			{
				return;
			}

			_fireTimer -= intervalSeconds;

			Entity player = default;
			var found = false;

			World.Query(
				in _playerQuery,
				(Entity entity) =>
				{
					if (!found)
					{
						player = entity;
						found = true;
					}
				}
			);

			if (!found)
			{
				return;
			}

			var targetPosition = World.Get<Transform>(player).Position;

			EnemyBulletFactory.AimedFire(World, boss, targetPosition, BulletSpeed);
		}
	}
}
