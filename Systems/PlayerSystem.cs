using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace CherryBomb.Systems
{
	public class PlayerSystem(World world, State state) : SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private readonly QueryDescription _playerEntities = new QueryDescription().WithAll<
			Direction,
			TagPlayer,
			Transform
		>();

		// ~7 shots/sec.
		private readonly Timer _bulletTimer = new(0.133f);

		public override void Update(in GameTime gameTime)
		{
			if (_state.GameOver)
			{
				return;
			}

			_bulletTimer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			World.Query(
				in _playerEntities,
				(Entity entity) =>
				{
					var direction = World.Get<Direction>(entity);
					var transform = World.Get<Transform>(entity);

					direction.X = 0;
					direction.Y = 0;

					if (
						GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadLeft)
						|| Keyboard.GetState().IsKeyDown(Keys.Left)
					)
					{
						direction.X = -1;
					}
					else if (
						GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadRight)
						|| Keyboard.GetState().IsKeyDown(Keys.Right)
					)
					{
						direction.X = 1;
					}

					if (
						GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadUp)
						|| Keyboard.GetState().IsKeyDown(Keys.Up)
					)
					{
						direction.Y = -1;
					}
					else if (
						GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.DPadDown)
						|| Keyboard.GetState().IsKeyDown(Keys.Down)
					)
					{
						direction.Y = 1;
					}

					if (
						(
							GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.A)
							|| Keyboard.GetState().IsKeyDown(Keys.Z)
						) && _bulletTimer.IsExpired
					)
					{
						_bulletTimer.Reset();
						World.Create(
							new BoxCollider(
								SpriteSheet.Bullet.BoxCollider.Width,
								SpriteSheet.Bullet.BoxCollider.Height
							),
							new CollisionLayer(CollisionMasks.PlayerProjectile),
							new CollisionMask(CollisionMasks.Enemy),
							new Direction(0, -1),
							new Sprite(SpriteSheet.Bullet.Frame),
							new TagBullet(),
							new Transform(
								transform.Position + new Vector2(1, -8),
								0f,
								new Vector2(1f, 1f)
							),
							new Velocity(0, 120)
						);

						SoundSystem.Play(World, "shoot");
					}
				}
			);
		}
	}
}
