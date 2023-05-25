using Components;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class MovementSystem : EntityUpdateSystem
	{
		private ComponentMapper<Direction> _directionManager;
		private ComponentMapper<Transform> _transformMapper;
		private ComponentMapper<Velocity> _velocityMapper;

		public MovementSystem() : base(Aspect.All(typeof(Direction), typeof(Transform), typeof(Velocity)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_directionManager = mapperService.GetMapper<Direction>();
			_transformMapper = mapperService.GetMapper<Transform>();
			_velocityMapper = mapperService.GetMapper<Velocity>();
		}

		public override void Update(GameTime gameTime)
		{
			foreach (var entity in ActiveEntities)
			{
				var direction = _directionManager.Get(entity);
				var transform = _transformMapper.Get(entity);
				var velocity = _velocityMapper.Get(entity);

				// TODO: Need a class here that's compatible with MonoGame.Extended.Vector2
				var velocityVector = new Vector2(velocity.X, velocity.Y);
				var directionVector = new Vector2(direction.X, direction.Y);


				transform.Position += velocityVector * directionVector * (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
		}
	}
}