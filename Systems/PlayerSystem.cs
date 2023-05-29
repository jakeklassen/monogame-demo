using CherryBomb;
using Components;
using Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;


namespace Systems
{
	public class PlayerSystem : EntityUpdateSystem
	{
		private ComponentMapper<Direction> _directionManager;
		private ComponentMapper<Transform> _transformMapper;
		private readonly Timer _bulletTimer;

		public PlayerSystem() : base(Aspect.All(typeof(Direction), typeof(TagPlayer), typeof(Transform)))
		{
			_bulletTimer = new Timer(0.133f);
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_directionManager = mapperService.GetMapper<Direction>();
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Update(GameTime gameTime)
		{
			_bulletTimer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			foreach (var entity in ActiveEntities)
			{
				var direction = _directionManager.Get(entity);
				var transform = _transformMapper.Get(entity);

				direction.X = 0;
				direction.Y = 0;

				if (Keyboard.GetState().IsKeyDown(Keys.Left))
				{
					direction.X = -1;
				}
				else if (Keyboard.GetState().IsKeyDown(Keys.Right))
				{
					direction.X = 1;
				}

				if (Keyboard.GetState().IsKeyDown(Keys.Up))
				{
					direction.Y = -1;
				}
				else if (Keyboard.GetState().IsKeyDown(Keys.Down))
				{
					direction.Y = 1;
				}

				if (Keyboard.GetState().IsKeyDown(Keys.X) && _bulletTimer.IsExpired)
				{
					_bulletTimer.Reset();
					var bullet = CreateEntity();

					bullet.Attach(new BoxCollider(6, 8));
					bullet.Attach(new CollisionLayer(CollisionMasks.PlayerProjectile));
					bullet.Attach(new CollisionMask(CollisionMasks.Enemy));
					bullet.Attach(new Direction(0, -1));
					bullet.Attach(new Sprite(new Rectangle(0, 8, 6, 8)));
					bullet.Attach(new TagBullet());
					bullet.Attach(new Transform(transform.Position + new Vector2(1, -8), 0f, new Vector2(1f, 1f)));
					bullet.Attach(new Velocity(0, 120));
				}
			}
		}
	}
}