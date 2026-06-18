using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using Arch.Core.Extensions;
using CherryBomb;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace CherryBomb.Systems
{
	public class CollisionSystem(World world, Config config) : SystemBase<GameTime>(world)
	{
		private readonly Config _config = config;
		private readonly QueryDescription _collidables = new QueryDescription().WithAll<
			BoxCollider,
			CollisionLayer,
			CollisionMask,
			Transform
		>();
		private readonly List<int> _handledEntities = [];

		public override void Update(in GameTime gameTime)
		{
			_handledEntities.Clear();

			var entities = new Entity[World.CountEntities(in _collidables)];
			World.GetEntities(in _collidables, entities, 0);

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

					var aabb = new RectangleF(x, y, boxCollider.Width, boxCollider.Height);

					var otherAabb = new RectangleF(
						otherX,
						otherY,
						otherBoxCollider.Width,
						otherBoxCollider.Height
					);

					if (aabb.Intersects(otherAabb) == false)
					{
						continue;
					}

					Entity? player =
						World.Has<TagPlayer>(entity) ? entity
						: World.Has<TagPlayer>(otherEntity) ? otherEntity
						: null;

					Entity? enemy =
						World.Has<TagEnemy>(entity) ? entity
						: World.Has<TagEnemy>(otherEntity) ? otherEntity
						: null;

					Entity? playerProjectile =
						World.Has<TagBullet>(entity) ? entity
						: World.Has<TagBullet>(otherEntity) ? otherEntity
						: null;

					Entity? enemyBullet =
						World.Has<TagEnemyBullet>(entity) ? entity
						: World.Has<TagEnemyBullet>(otherEntity) ? otherEntity
						: null;

					if (AssertIsNotNull(enemy) && AssertIsNotNull(playerProjectile))
					{
						var damage = _config.Entities.Player.Projectiles.Bullet.Damage;

						World.Create(
							// For some reason this is not working, I had to cast
							new EventPlayerProjectileEnemyCollision(
								(Entity)playerProjectile,
								(Entity)enemy,
								damage
							)
						);
					}
					else if (AssertIsNotNull(player))
					{
						// The player only takes damage when not already invulnerable.
						// This is where the skip-while-invulnerable rule lives.
						if (!World.Has<Invulnerable>((Entity)player))
						{
							// Touching an enemy bullet or an enemy itself both hurt.
							Entity? damageSource =
								enemyBullet ?? (AssertIsNotNull(enemy) ? enemy : null);

							if (AssertIsNotNull(damageSource))
							{
								World.Create(
									new EventPlayerEnemyCollision(
										(Entity)player,
										(Entity)damageSource
									)
								);
							}
						}
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
