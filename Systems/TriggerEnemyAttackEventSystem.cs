using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using CherryBomb.Lib;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Consumes EventTriggerEnemyAttack: 50% of the time it does nothing; otherwise it
	// picks a protect-state enemy near the front of the formation and switches it into
	// its attack run (excluding the boss). Source: trigger-enemy-attack-event-system.ts.
	public class TriggerEnemyAttackEventSystem(World world, Scheduler scheduler)
		: SystemBase<GameTime>(world)
	{
		private const int Boss = 5;

		private readonly Scheduler _scheduler = scheduler;

		private readonly QueryDescription _eventQuery =
			new QueryDescription().WithAll<EventTriggerEnemyAttack>();
		private readonly QueryDescription _enemyQuery = new QueryDescription().WithAll<
			EnemyState,
			EnemyType,
			SpriteAnimation,
			TagEnemy,
			Transform
		>();

		private readonly List<Entity> _events = [];
		private readonly List<Entity> _enemies = [];

		public override void Update(in GameTime gameTime)
		{
			_events.Clear();
			World.Query(in _eventQuery, (Entity entity) => _events.Add(entity));

			foreach (var @event in _events)
			{
				World.Destroy(@event);

				// Only trigger 50% of the time.
				if (Random.Shared.NextDouble() < 0.5)
				{
					continue;
				}

				_enemies.Clear();
				World.Query(in _enemyQuery, (Entity entity) => _enemies.Add(entity));

				var pickable = EnemyAi.DeterminePickableEnemies(World, _enemies);
				var enemy = EnemyAi.PickRandomEnemy(pickable, 10);

				if (enemy == null)
				{
					continue;
				}

				var enemyType = World.Get<EnemyType>(enemy.Value).Value;

				if (enemyType == Boss)
				{
					continue;
				}

				EnemyAi.SwitchEnemyToAttackMode(World, enemy.Value, _scheduler);
			}
		}
	}
}
