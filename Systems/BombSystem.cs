using System.Collections.Generic;
using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.Lib;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Consumes EventTriggerBomb. When a wave is ready and the bomb is not locked,
	// locks it and ripple-spawns a TagBomb projectile (PlayerProjectile -> Enemy,
	// huge damage) at each enemy's collider center, staggered by 50*idx ms via the
	// scheduler. Source: bomb-system.ts.
	public class BombSystem(World world, State state, Scheduler scheduler)
		: SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private readonly Scheduler _scheduler = scheduler;

		private readonly QueryDescription _bombEvents =
			new QueryDescription().WithAll<EventTriggerBomb>();
		private readonly QueryDescription _enemies = new QueryDescription().WithAll<
			BoxCollider,
			TagEnemy,
			Transform
		>();

		public override void Update(in GameTime gameTime)
		{
			var hasEvent = false;

			// Drain all bomb events first (there should only ever be one).
			World.Query(
				in _bombEvents,
				(Entity entity) =>
				{
					hasEvent = true;
					World.Destroy(entity);
				}
			);

			if (!hasEvent || !_state.WaveReady || _state.BombLocked)
			{
				return;
			}

			_state.BombLocked = true;

			// Snapshot enemy centers now; the scheduled callbacks run later when the
			// enemies may already be gone.
			var centers = new List<Vector2>();

			World.Query(
				in _enemies,
				(Entity entity, ref BoxCollider collider, ref Transform transform) =>
				{
					centers.Add(
						new Vector2(
							transform.Position.X + collider.Offset.X + (collider.Width / 2f),
							transform.Position.Y + collider.Offset.Y + (collider.Height / 2f)
						)
					);
				}
			);

			for (var idx = 0; idx < centers.Count; idx++)
			{
				var center = centers[idx];

				_scheduler.Add(
					50f * idx,
					() =>
					{
						World.Create(
							new BoxCollider(8, 8),
							new CollisionLayer(CollisionMasks.PlayerProjectile),
							new CollisionMask(CollisionMasks.Enemy),
							new TagBomb(),
							// Self-clean: a bomb spawned where an enemy already died has
							// no target and would otherwise linger (it has no Velocity).
							new TimeToLive(0.25f),
							new Transform(center, 0f, Vector2.One)
						);
					}
				);
			}
		}
	}
}
