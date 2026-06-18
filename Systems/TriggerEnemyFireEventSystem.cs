using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Consumes EventTriggerEnemyFire and makes an enemy shoot. A protect-state yellow
	// ship gets a 50% chance to fire a spread first; otherwise a random front-row enemy
	// fires per type: yellow ship -> spread, red flame guy -> aimed, others -> straight
	// down. Boss excluded. Source: trigger-enemy-fire-event-system.ts.
	public class TriggerEnemyFireEventSystem(World world) : SystemBase<GameTime>(world)
	{
		private const int RedFlameGuy = 2;
		private const int YellowShip = 4;
		private const int Boss = 5;

		private readonly QueryDescription _eventQuery =
			new QueryDescription().WithAll<EventTriggerEnemyFire>();
		private readonly QueryDescription _enemyQuery = new QueryDescription().WithAll<
			EnemyState,
			EnemyType,
			SpriteAnimation,
			TagEnemy,
			Transform
		>();
		private readonly QueryDescription _playerQuery = new QueryDescription().WithAll<
			TagPlayer,
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

				_enemies.Clear();
				World.Query(in _enemyQuery, (Entity entity) => _enemies.Add(entity));

				// Each event gives the protect-state yellow ships a chance to spread.
				// First one that rolls under 50% fires and short-circuits this event.
				var spreadFired = false;

				foreach (var enemy in _enemies)
				{
					var type = World.Get<EnemyType>(enemy).Value;
					var stateValue = World.Get<EnemyState>(enemy).Value;

					if (type != YellowShip || stateValue != EnemyStateType.Protect)
					{
						continue;
					}

					if (Random.Shared.NextDouble() < 0.5)
					{
						EnemyBulletFactory.FireSpread(World, enemy, count: 12, speed: 40f);
						spreadFired = true;

						break;
					}
				}

				if (spreadFired)
				{
					continue;
				}

				var pickable = EnemyAi.DeterminePickableEnemies(World, _enemies);
				var picked = EnemyAi.PickRandomEnemy(pickable, 10);

				if (picked == null)
				{
					continue;
				}

				var enemyEntity = picked.Value;
				var enemyType = World.Get<EnemyType>(enemyEntity).Value;

				if (enemyType == Boss)
				{
					continue;
				}

				if (enemyType == YellowShip)
				{
					EnemyBulletFactory.FireSpread(World, enemyEntity, count: 12, speed: 40f);
				}
				else if (enemyType == RedFlameGuy)
				{
					var player = GetPlayer();

					if (player == null)
					{
						continue;
					}

					var targetPosition = World.Get<Transform>(player.Value).Position;

					EnemyBulletFactory.AimedFire(World, enemyEntity, targetPosition, speed: 60f);
				}
				else
				{
					EnemyBulletFactory.Fire(
						World,
						enemyEntity,
						angleDegrees: 0f,
						speed: 60f,
						triggerSound: true
					);
				}
			}
		}

		private Entity? GetPlayer()
		{
			Entity? player = null;

			World.Query(in _playerQuery, (Entity entity) => player ??= entity);

			return player;
		}
	}
}
