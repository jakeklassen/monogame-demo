using System.Diagnostics;
using CherryBomb;
using Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class PlayerProjectileEnemyCollisionEventSystem : EntityProcessingSystem
	{
		private ComponentMapper<EventPlayerProjectileEnemyCollision> _eventPlayerProjectileEnemyCollisionMapper;

		public PlayerProjectileEnemyCollisionEventSystem() : base(Aspect.All(typeof(EventPlayerProjectileEnemyCollision)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_eventPlayerProjectileEnemyCollisionMapper = mapperService.GetMapper<EventPlayerProjectileEnemyCollision>();
		}

		public override void Process(GameTime gameTime, int entityId)
		{
			var collisionEvent = _eventPlayerProjectileEnemyCollisionMapper.Get(entityId);

			Debug.WriteLine($"Processing collision event for entity {collisionEvent.EnemyEntityId} and projectile {collisionEvent.ProjectileEntityId}");

			var projectile = GetEntity(collisionEvent.ProjectileEntityId);
			var projectileTransform = projectile.Get<Transform>();

			var bulletShockwaveEntity = CreateEntity();

			bulletShockwaveEntity.Attach(new Shockwave(
				radius: 3,
				targetRadius: 6,
				color: Pico8Color.Color9,
				speed: 30
			));

			bulletShockwaveEntity.Attach(new Transform(
				position: new Vector2(projectileTransform.Position.X + 4, projectileTransform.Position.Y + 4),
				rotation: 0,
				scale: Vector2.One
			));

			// Destory the bullet
			DestroyEntity(collisionEvent.ProjectileEntityId);

			// TODO: Spawn a shockwave

			// TODO: Spawn an explosion

			DestroyEntity(entityId);
		}
	}
}