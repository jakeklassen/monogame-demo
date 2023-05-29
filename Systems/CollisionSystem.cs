using System.Collections.Generic;
using Components;

using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class CollisionSystem : EntityUpdateSystem
	{
		private ComponentMapper<BoxCollider> _boxColliderMapper;
		private ComponentMapper<CollisionLayer> _collisionLayerMapper;
		private ComponentMapper<CollisionMask> _collisionMaskMapper;
		private ComponentMapper<Transform> _transformMapper;
		private readonly List<int> _handledEntities;

		public CollisionSystem()
			: base(
				Aspect.All(
					typeof(BoxCollider),
					typeof(CollisionLayer),
					typeof(CollisionMask),
					typeof(Transform))
				.Exclude(typeof(TagInactive))
			)
		{
			_handledEntities = new List<int>();
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_boxColliderMapper = mapperService.GetMapper<BoxCollider>();
			_collisionLayerMapper = mapperService.GetMapper<CollisionLayer>();
			_collisionMaskMapper = mapperService.GetMapper<CollisionMask>();
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Update(GameTime gameTime)
		{
			_handledEntities.Clear();

			foreach (var entityId in ActiveEntities)
			{
				if (_handledEntities.Contains(entityId))
				{
					continue;
				}

				foreach (var otherEntityId in ActiveEntities)
				{
					if (entityId == otherEntityId)
					{
						continue;
					}

					if (_handledEntities.Contains(otherEntityId))
					{
						continue;
					}

					BoxCollider boxCollider = _boxColliderMapper.Get(entityId);
					CollisionLayer collisionLayer = _collisionLayerMapper.Get(entityId);
					Transform transform = _transformMapper.Get(entityId);

					BoxCollider otherBoxCollider = _boxColliderMapper.Get(otherEntityId);
					CollisionMask otherCollisionMask = _collisionMaskMapper.Get(otherEntityId);
					Transform otherTransform = _transformMapper.Get(otherEntityId);

					// Make sure entityA's collision layer is a subset of entityB's
					// collision mask
					if (
						(collisionLayer.Value & otherCollisionMask.Value) !=
						collisionLayer.Value
					)
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

					var entity = GetEntity(entityId);
					var otherEntity = GetEntity(otherEntityId);

					var enemy = entity.Has<TagEnemy>()
						? entity
						: otherEntity.Has<TagEnemy>()
						? otherEntity
						: null;

					var playerProjectile = entity.Has<TagBullet>()
						? entity
						: otherEntity.Has<TagBullet>()
						? otherEntity
						: null;

					if (playerProjectile != null && enemy != null)
					{
						var eventEntity = CreateEntity();

						// I HATE this but we need to somehow tag the entities as inactive
						// otherwise we'll pick up these entities for collision again when the
						// next frame comes around.
						// Maybe replace with system specific tags? e.g. TagCollisionInactive
						playerProjectile.Attach(new TagInactive());
						enemy.Attach(new TagInactive());

						eventEntity.Attach(new EventPlayerProjectileEnemyCollision(playerProjectile.Id, enemy.Id));
					}

					_handledEntities.Add(entityId);
					_handledEntities.Add(otherEntityId);

					// We're done with this entity, so break out of the loop
					break;
				}
			}
		}
	}
}