using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace Systems
{
	public class CollisionSystem : SystemBase<GameTime>
	{
		private readonly QueryDescription _collidables = new QueryDescription()
			.WithAll<BoxCollider, CollisionLayer, CollisionMask, Transform>()
			.WithNone<Invulnerable>();
		private readonly List<int> _handledEntities;

		public CollisionSystem(World world)
			: base(world)
		{
			_handledEntities = new List<int>();
		}

		public override void Update(in GameTime gameTime)
		{
			_handledEntities.Clear();

			var entities = new List<Entity>();
			World.GetEntities(_collidables, entities);

			foreach (var entity in entities)
			{
				if (_handledEntities.Contains(entity.Id))
				{
					continue;
				}

				foreach (var otherEntity in entities)
				{
					if (entity.Id == otherEntity.Id)
					{
						continue;
					}

					if (_handledEntities.Contains(otherEntity.Id))
					{
						continue;
					}

					BoxCollider boxCollider = World.Get<BoxCollider>(entity);
					CollisionLayer collisionLayer = World.Get<CollisionLayer>(entity);
					Transform transform = World.Get<Transform>(entity);

					BoxCollider otherBoxCollider = World.Get<BoxCollider>(otherEntity);
					CollisionMask otherCollisionMask = World.Get<CollisionMask>(otherEntity);
					Transform otherTransform = World.Get<Transform>(otherEntity);

					// Make sure entityA's collision layer is a subset of entityB's
					// collision mask
					if ((collisionLayer.Value & otherCollisionMask.Value) != collisionLayer.Value)
					{
						continue;
					}

					var x = transform.Position.X + boxCollider.Offset.X;
					var y = transform.Position.Y + boxCollider.Offset.Y;
					var otherX = otherTransform.Position.X + otherBoxCollider.Offset.X;
					var otherY = otherTransform.Position.Y + otherBoxCollider.Offset.Y;

					var aabb = new RectangleF(
						x - (boxCollider.Width / 2),
						y - (boxCollider.Height / 2),
						boxCollider.Width,
						boxCollider.Height
					);

					var otherAabb = new RectangleF(
						otherX - (otherBoxCollider.Width / 2),
						otherY - (otherBoxCollider.Height / 2),
						otherBoxCollider.Width,
						otherBoxCollider.Height
					);

					if (aabb.Intersects(otherAabb) == false)
					{
						continue;
					}

					Entity? enemy = World.Has<TagEnemy>(entity)
						? entity
						: World.Has<TagEnemy>(otherEntity)
							? otherEntity
							: null;

					Entity? playerProjectile = World.Has<TagBullet>(entity)
						? entity
						: World.Has<TagBullet>(otherEntity)
							? otherEntity
							: null;

					if (AssertIsNotNull(enemy) && AssertIsNotNull(playerProjectile))
					{
						World.Create(
							// For some reason this is not working, I had to cast
							new EventPlayerProjectileEnemyCollision((Entity)playerProjectile, (Entity)enemy)
						);

						// I HATE this but we need to somehow tag the entities as inactive
						// otherwise we'll pick up these entities for collision again when the
						// next frame comes around.
						// Maybe replace with system specific tags? e.g. TagCollisionInactive
						// World.Add(playerProjectile, new TagInactive());
						// World.Add(enemy, new TagInactive());
					}

					_handledEntities.Add(entity.Id);
					_handledEntities.Add(otherEntity.Id);

					// We're done with this entity, so break out of the loop
					break;
				}
			}
		}

		private static bool AssertIsNotNull([NotNullWhen(true)] Entity? entity)
		{
			return entity is not null;
		}
	}
}
