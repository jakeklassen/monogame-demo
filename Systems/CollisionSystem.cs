using System.Collections.Generic;
using System.Diagnostics;
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
				Aspect.All(typeof(BoxCollider),
				typeof(CollisionLayer),
				typeof(CollisionMask),
				typeof(Transform))
				.Exclude(typeof(TagPlayer))
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
			Debug.WriteLine("handled entities cleared");
			_handledEntities.Clear();

			foreach (var entityId in ActiveEntities)
			{
				if (_handledEntities.Contains(entityId))
				{
					Debug.WriteLine($"Skipping entity {entityId}");

					continue;
				}

				foreach (var otherEntityId in ActiveEntities)
				{
					if (entityId == otherEntityId)
					{
						Debug.WriteLine($"Skipping entity {entityId} because it's the same as {otherEntityId}");
						continue;
					}

					if (_handledEntities.Contains(otherEntityId))
					{
						Debug.WriteLine($"Skipping entity {otherEntityId} because it's already been handled");
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

					var playerBullet = entity.Has<TagBullet>()
						? entity
						: otherEntity.Has<TagBullet>()
						? otherEntity
						: null;

					if (playerBullet != null && enemy != null)
					{
						Debug.WriteLine("Player bullet hit enemy");

						var eventEntity = CreateEntity();

						eventEntity.Attach(new EventPlayerProjectileEnemyCollision(playerBullet.Id, enemy.Id));
					}

					_handledEntities.Add(entityId);
					_handledEntities.Add(otherEntityId);

					Debug.WriteLine($"Tracked {entityId} and {otherEntityId}");

					// We're done with this entity, so break out of the loop
					break;
				}
			}
		}
	}
}