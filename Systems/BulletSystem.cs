using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;

namespace CherryBomb.Systems
{
	// Advance bullets, home the homing ones, test them against live enemies, and
	// reap the spent ones. Ported EXACTLY from space-drift/sim.ts bulletSystem +
	// hitEnemy.
	//
	// Entities are collected into plain lists first, then walked with a normal
	// loop — the collision test nests bullets × enemies and hitEnemy spawns
	// particles (a structural change), neither of which is safe inside a live
	// Arch query.
	public sealed class BulletSystem(World world)
	{
		private const float DegToRad = MathF.PI / 180f;

		private readonly World _world = world;
		private readonly Random _rng = new();

		private readonly QueryDescription _bulletQuery = new QueryDescription().WithAll<
			Bullet,
			Transform,
			Previous,
			Velocity
		>();
		private readonly QueryDescription _enemyQuery = new QueryDescription().WithAll<
			Enemy,
			Transform
		>();

		private readonly List<Entity> _bullets = [];
		private readonly List<Entity> _enemies = [];
		private readonly List<Entity> _dead = [];

		public void Update(float dt)
		{
			_bullets.Clear();
			_enemies.Clear();
			_dead.Clear();

			_world.Query(in _bulletQuery, (Entity e, ref Bullet b) => _bullets.Add(e));
			_world.Query(
				in _enemyQuery,
				(Entity e, ref Enemy en) =>
				{
					if (en.RespawnTimer <= 0f)
						_enemies.Add(e);
				}
			);

			foreach (var be in _bullets)
			{
				ref var tf = ref _world.Get<Transform>(be);
				ref var prev = ref _world.Get<Previous>(be);
				ref var vel = ref _world.Get<Velocity>(be);
				ref var bu = ref _world.Get<Bullet>(be);

				prev.Position = tf.Position;
				prev.Rotation = tf.Rotation;

				bu.Age += dt;
				if (bu.Age >= bu.MaxAge)
				{
					_dead.Add(be);
					continue;
				}

				bool isHoming = _world.Has<Homing>(be);

				// Homing: after a brief straight "launch" phase (so the volley fans out
				// first), steer velocity toward the target — the turn rate ramps up as
				// it closes, so it tightens onto the target instead of orbiting it.
				if (isHoming && bu.Age >= Constants.HomingSeekDelay)
				{
					ref var hom = ref _world.Get<Homing>(be);
					var target = hom.Target;
					bool targetLive =
						_world.IsAlive(target)
						&& _world.Has<Transform>(target)
						&& (
							!_world.Has<Enemy>(target)
							|| _world.Get<Enemy>(target).RespawnTimer <= 0f
						);
					if (targetLive)
					{
						ref var ttf = ref _world.Get<Transform>(target);
						float dx = ttf.Position.X - tf.Position.X;
						float dy = ttf.Position.Y - tf.Position.Y;
						float dist = MathF.Sqrt(dx * dx + dy * dy);
						if (dist == 0f)
							dist = 1f;
						float closeBoost =
							1f
							+ Constants.HomingTurnCloseBoost
								* MathF.Max(
									0f,
									(Constants.HomingCloseDist - dist) / Constants.HomingCloseDist
								);
						float cur = MathF.Atan2(vel.Value.Y, vel.Value.X);
						float diff = MathF.Atan2(dy, dx) - cur;
						diff = MathF.Atan2(MathF.Sin(diff), MathF.Cos(diff)); // wrap [-π, π]
						float maxStep = hom.TurnRate * closeBoost * DegToRad * dt;
						float next = cur + Math.Clamp(diff, -maxStep, maxStep);
						float speed = MathF.Sqrt(
							vel.Value.X * vel.Value.X + vel.Value.Y * vel.Value.Y
						);
						vel.Value.X = MathF.Cos(next) * speed;
						vel.Value.Y = MathF.Sin(next) * speed;
						// Point the sprite along travel (sprite's "up" is -y).
						tf.Rotation = MathF.Atan2(MathF.Cos(next), -MathF.Sin(next)) / DegToRad;
					}
				}

				tf.Position.X += vel.Value.X * dt;
				tf.Position.Y += vel.Value.Y * dt;

				// Homing missiles get a small proximity fuse so a tight pass still lands.
				float hitR =
					Constants.EnemyRadius
					+ Constants.BulletRadius
					+ (isHoming ? Constants.HomingProximity : 0f);

				foreach (var ee in _enemies)
				{
					if (!_world.IsAlive(ee))
						continue;
					ref var en = ref _world.Get<Enemy>(ee);
					if (en.RespawnTimer > 0f)
						continue;
					ref var etf = ref _world.Get<Transform>(ee);
					float dx = etf.Position.X - tf.Position.X;
					float dy = etf.Position.Y - tf.Position.Y;
					if (dx * dx + dy * dy <= hitR * hitR)
					{
						HitEnemy(ee, tf.Position.X, tf.Position.Y);
						_dead.Add(be);
						break;
					}
				}
			}

			foreach (var be in _dead)
			{
				if (_world.IsAlive(be))
					_world.Destroy(be);
			}
		}

		private float RndRange(float a, float b) => a + _rng.NextSingle() * (b - a);

		// Apply a hit: flash, spark, and on death a burst plus a respawn countdown.
		private void HitEnemy(Entity enemyEntity, float atX, float atY)
		{
			ref var en = ref _world.Get<Enemy>(enemyEntity);
			en.Health -= 1;
			en.HitFlash = Constants.EnemyHitFlash;

			for (int i = 0; i < 6; i++)
			{
				float angle = RndRange(0f, MathF.PI * 2f);
				float speed = RndRange(20f, 70f);
				Factories.CreateParticle(
					_world,
					atX,
					atY,
					MathF.Cos(angle) * speed,
					MathF.Sin(angle) * speed,
					RndRange(0.1f, 0.25f),
					ParticleKind.Flame
				);
			}

			if (en.Health <= 0)
			{
				var pos = _world.Get<Transform>(enemyEntity).Position;
				for (int i = 0; i < 24; i++)
				{
					float angle = RndRange(0f, MathF.PI * 2f);
					float speed = RndRange(30f, 120f);
					Factories.CreateParticle(
						_world,
						pos.X,
						pos.Y,
						MathF.Cos(angle) * speed,
						MathF.Sin(angle) * speed,
						RndRange(0.2f, 0.5f),
						_rng.NextSingle() < 0.5f ? ParticleKind.Flame : ParticleKind.Smoke
					);
				}
				// Re-fetch: the particle creates above are structural changes.
				_world.Get<Enemy>(enemyEntity).RespawnTimer = Constants.EnemyRespawnDelay;
			}
		}
	}
}
