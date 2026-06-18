using System;
using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Drives the per-wave attack/fire cadence once the wave is in formation. Emits
	// EventTriggerEnemyAttack every wave.AttackFrequency seconds and
	// EventTriggerEnemyFire every wave.FireFrequency + rnd(FireFrequency) seconds.
	// The actual enemy selection / behaviour lives in the event-handler systems.
	// Source: systems/enemy-pick-system.ts. Replaces the M1 TriggerEnemyFireSystem
	// placeholder. Frequencies in Config are already expressed in seconds.
	public class EnemyPickSystem(World world, State state, Config config)
		: SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private readonly Config _config = config;

		private float _attackFrequencyTimer;
		private float _fireFrequencyTimer;
		private float _nextFireTime;

		public override void Update(in GameTime gameTime)
		{
			if (_state.GameOver || !_state.WaveReady)
			{
				return;
			}

			_config.Waves.TryGetValue(_state.Wave, out var wave);

			if (wave == null)
			{
				return;
			}

			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			_attackFrequencyTimer += dt;
			_fireFrequencyTimer += dt;

			if (_attackFrequencyTimer >= wave.AttackFrequency)
			{
				_attackFrequencyTimer = 0f;

				World.Create(new EventTriggerEnemyAttack());
			}

			if (_fireFrequencyTimer > _nextFireTime)
			{
				_fireFrequencyTimer = 0f;
				_nextFireTime =
					wave.FireFrequency + ((float)Random.Shared.NextDouble() * wave.FireFrequency);

				World.Create(new EventTriggerEnemyFire());
			}
		}
	}
}
