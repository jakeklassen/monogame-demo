using Arch.Core;
using CherryBomb;
using Components;
using Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Systems
{
	public class PlayerSystem : SystemBase<GameTime>
	{
		private readonly QueryDescription _playerEntities = new QueryDescription().WithAll<
			Direction,
			TagPlayer,
			Transform
		>();
		private readonly Timer _bulletTimer;

		public PlayerSystem(World world)
			: base(world)
		{
			_bulletTimer = new Timer(0.133f);
		}

		public override void Update(in GameTime gameTime)
		{
			_bulletTimer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			World.Query(
				in _playerEntities,
				(in Entity entity) =>
				{
					var direction = World.Get<Direction>(entity);
					var transform = World.Get<Transform>(entity);

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
						World.Create(
							new BoxCollider(6, 8),
							new CollisionLayer(CollisionMasks.PlayerProjectile),
							new CollisionMask(CollisionMasks.Enemy),
							new Direction(0, -1),
							new Sprite(new Rectangle(0, 8, 6, 8)),
							new TagBullet(),
							new Transform(transform.Position + new Vector2(1, -8), 0f, new Vector2(1f, 1f)),
							new Velocity(0, 120)
						);
					}
				}
			);
		}
	}
}
