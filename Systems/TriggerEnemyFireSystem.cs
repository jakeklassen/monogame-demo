using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Basic enemy fire cadence for M1: once the wave is in formation, periodically
	// picks a random formation enemy and fires a single bullet straight down. Full
	// attack-mode AI (aimed fire, spreads, attack runs) is M3. The cadence interval
	// is derived from the per-wave FireFrequency in Config.
	public class TriggerEnemyFireSystem(World world, State state, Config config)
		: SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private readonly Config _config = config;
		private readonly Random _random = new();

		// Formation enemies eligible to fire: alive, in the world, no longer flying in
		// (the fly-in tween grants Invulnerable which is dropped once WaveReady).
		private readonly QueryDescription _enemyQuery = new QueryDescription()
			.WithAll<TagEnemy, Transform, Sprite>()
			.WithNone<Invulnerable>();

		private float _accumulatorSeconds;

		public override void Update(in GameTime gameTime)
		{
			if (_state.GameOver || !_state.WaveReady)
			{
				_accumulatorSeconds = 0f;

				return;
			}

			_config.Waves.TryGetValue(_state.Wave, out var wave);

			if (wave == null || wave.FireFrequency <= 0f)
			{
				return;
			}

			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_accumulatorSeconds += dt;

			// FireFrequency is expressed in shots-per-30-frames units in Config; using
			// its reciprocal as the interval in seconds gives a comfortable cadence
			// (e.g. 20/30 -> ~1.5s, 10/30 -> ~3s).
			var intervalSeconds = 1f / wave.FireFrequency;

			if (_accumulatorSeconds < intervalSeconds)
			{
				return;
			}

			_accumulatorSeconds = 0f;

			var enemies = new List<Entity>(World.CountEntities(in _enemyQuery));

			World.Query(in _enemyQuery, (Entity entity) => enemies.Add(entity));

			if (enemies.Count == 0)
			{
				return;
			}

			var enemy = enemies[_random.Next(enemies.Count)];
			var transform = World.Get<Transform>(enemy);
			var sprite = World.Get<Sprite>(enemy);

			var firePosition = new Vector2(
				transform.Position.X + (sprite.CurrentFrame.Width / 2f) - 4f,
				transform.Position.Y + sprite.CurrentFrame.Height
			);

			// angle 0 == straight down. Aimed/spread fire arrives in M3.
			EnemyBulletFactory.Fire(World, firePosition, angleRadians: 0f, speed: 60f);
		}
	}
}
