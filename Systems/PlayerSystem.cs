using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
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

		// The player thruster is the only Parent-attached child in gameplay; used to
		// also flash it on spread-shot.
		private readonly QueryDescription _thrusterEntities = new QueryDescription().WithAll<
			Parent,
			Sprite
		>();

		// ~7 shots/sec.
		private readonly Timer _bulletTimer = new(0.133f);

		// Edge-trigger state for the spread-shot button (X / gamepad B).
		private bool _spreadShotHeld = false;

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

					// Spread-shot: X (keyboard) / B (gamepad), edge-triggered so one
					// press fires once. Spends all cherries when available.
					var spreadShotDown =
						GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.B)
						|| Keyboard.GetState().IsKeyDown(Keys.X);

					if (spreadShotDown && !_spreadShotHeld)
					{
						if (_state.Cherries > 0)
						{
							var thruster = FindThruster(entity);

							SpreadShotFactory.Fire(
								World,
								player: entity,
								thruster: thruster,
								count: _state.Cherries,
								speed: 200f
							);

							SoundSystem.Play(World, "spread-shot");

							_state.Cherries = 0;
						}
						else
						{
							SoundSystem.Play(World, "no-spread-shot");
						}
					}

					_spreadShotHeld = spreadShotDown;
				}
			);
		}

		// Locates the player's thruster child (Parent.Entity == player). Returns the
		// player itself as a harmless fallback if none is found.
		private Entity FindThruster(Entity player)
		{
			var thruster = player;

			World.Query(
				in _thrusterEntities,
				(Entity entity, ref Parent parent) =>
				{
					if (parent.Entity == player)
					{
						thruster = entity;
					}
				}
			);

			return thruster;
		}
	}
}
