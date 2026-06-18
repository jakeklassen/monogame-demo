using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.Lib;
using Microsoft.Xna.Framework;

namespace CherryBomb.EntityFactories
{
	// Enemy attack-mode helpers ported from enemy/determine-pickable-enemies.ts,
	// enemy/pick-random-enemy.ts and enemy/switch-enemy-to-attach-mode.ts.
	public static class EnemyAi
	{
		private const int GreenAlien = 1;
		private const int RedFlameGuy = 2;
		private const int SpinningShip = 3;
		private const int YellowShip = 4;

		// Protect-state enemies sorted top-to-bottom, left-to-right (rows grouped),
		// so PickRandomEnemy draws from the front of the formation. Source:
		// determine-pickable-enemies.ts + entity/sort-entities-by-position.ts.
		public static List<Entity> DeterminePickableEnemies(
			World world,
			IReadOnlyList<Entity> enemies
		)
		{
			var pickable = new List<Entity>(enemies.Count);

			foreach (var enemy in enemies)
			{
				if (
					world.TryGet<EnemyState>(enemy, out var state)
					&& state?.Value == EnemyStateType.Protect
				)
				{
					pickable.Add(enemy);
				}
			}

			pickable.Sort(
				(a, b) =>
				{
					var pa = world.Get<Transform>(a).Position;
					var pb = world.Get<Transform>(b).Position;

					if (pa.Y < pb.Y)
					{
						return -1;
					}

					if (pa.Y > pb.Y)
					{
						return 1;
					}

					return pa.X.CompareTo(pb.X);
				}
			);

			return pickable;
		}

		// Picks a random enemy from the last `elementsFromLast` of the sorted list
		// (i.e. the front rows). Returns null when the list is empty. Source:
		// pick-random-enemy.ts.
		public static Entity? PickRandomEnemy(IReadOnlyList<Entity> enemies, int elementsFromLast)
		{
			if (enemies.Count == 0)
			{
				return null;
			}

			var max = Math.Min(enemies.Count, elementsFromLast);
			// rndInt(max, 1) -> inclusive 1..max
			var randomIndex = Random.Shared.Next(1, max + 1);
			var enemyIndex = enemies.Count - randomIndex;

			return enemies[enemyIndex];
		}

		// Switches an enemy into its attack run: state -> Attack, doubled animation
		// speed, edge-aware horizontal sway, and (after a 2s delay) per-type velocity
		// and behaviour tags. Source: switch-enemy-to-attach-mode.ts.
		public static void SwitchEnemyToAttackMode(World world, Entity enemy, Scheduler scheduler)
		{
			var state = world.Get<EnemyState>(enemy);
			state.Value = EnemyStateType.Attack;

			// Halve the per-frame interval => double the animation speed.
			if (world.Has<SpriteAnimation>(enemy))
			{
				var animation = world.Get<SpriteAnimation>(enemy);
				animation.FrameRate /= 2f;
			}

			var enemyType = world.Has<EnemyType>(enemy) ? world.Get<EnemyType>(enemy).Value : 0;
			var position = world.Get<Transform>(enemy).Position;
			var startDirection = Random.Shared.NextDouble() < 0.5 ? 1f : -1f;

			// Edge-aware horizontal sway: weave around an inward-pushed center so an
			// enemy starting near a wall doesn't immediately exit the play-field. Only
			// green aliens and red flame guys actually weave (matches the TS, which only
			// concatenates the sway tweens for those two types in the 2s callback);
			// spinning ships and yellow ships move straight / are driven by their own
			// systems, so we build the sway here but only attach it for green/red below.
			const float swayPeriod = 1.6f;
			const float swayAmplitude = 16f;

			var sway = new Sway()
			{
				Amplitude = swayAmplitude,
				Period = swayPeriod,
				Direction = startDirection,
				CenterX = position.X,
			};

			if (position.X < 16f)
			{
				// Started near the left wall: push the weave to the right.
				sway.CenterX = position.X + 24f;
				sway.Direction = -1f;
			}
			else if (position.X > 104f)
			{
				// Started near the right wall: push the weave to the left.
				sway.CenterX = position.X - 24f;
				sway.Direction = 1f;
			}

			// After a beat, commit to the dive. Per-type velocity / tags applied here.
			scheduler.Add(
				2000f,
				() =>
				{
					if (!world.IsAlive(enemy))
					{
						return;
					}

					var velocity = new Velocity(0f, 51f);

					if (enemyType == RedFlameGuy)
					{
						// The red guy is more aggressive: faster, tighter sway.
						velocity.Y = 75f;
						sway.Period = 0.8f;
						sway.Amplitude = 8f;
					}
					else if (enemyType == SpinningShip)
					{
						velocity.Y = 60f;

						if (!world.Has<TagLateralHunter>(enemy))
						{
							world.Add(enemy, new TagLateralHunter());
						}
					}
					else if (enemyType == YellowShip)
					{
						velocity.Y = 10f;

						if (!world.Has<TagYellowShip>(enemy))
						{
							world.Add(enemy, new TagYellowShip());
						}
					}

					// Only the green alien and red flame guy weave laterally.
					if (
						(enemyType == GreenAlien || enemyType == RedFlameGuy)
						&& !world.Has<Sway>(enemy)
					)
					{
						world.Add(enemy, sway);
					}

					if (world.Has<Direction>(enemy))
					{
						world.Set(enemy, new Direction(0, 1));
					}
					else
					{
						world.Add(enemy, new Direction(0, 1));
					}

					if (world.Has<Velocity>(enemy))
					{
						world.Set(enemy, velocity);
					}
					else
					{
						world.Add(enemy, velocity);
					}
				}
			);
		}
	}
}
