using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Handles player <-> pickup collisions emitted by CollisionSystem. Collecting a
	// cherry bumps State.Cherries, plays the pickup SFX, and spawns a pink shockwave.
	// Every 10th cherry resets the counter and grants either an extra life (+ "1UP!"
	// floating text + extra-life SFX) or +5000 score (+ "5000" floating text).
	public class PlayerPickupCollisionEventSystem(World world, State state)
		: SystemBase<GameTime>(world)
	{
		private readonly State _state = state;

		private readonly QueryDescription _events =
			new QueryDescription().WithAll<EventPlayerPickupCollision>();

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _events,
				(Entity entity, ref EventPlayerPickupCollision collisionEvent) =>
				{
					var pickup = collisionEvent.PickupEntity;

					// Remember where the cherry was so the floating text / shockwave can
					// be placed there before the pickup entity is destroyed.
					var pickupTransform = World.Get<Transform>(pickup);
					var pickupSprite = World.Get<Sprite>(pickup);
					var pickupPosition = pickupTransform.Position;

					World.Destroy(pickup);

					_state.Cherries++;

					var track = "pickup";
					string message = null;

					if (_state.Cherries >= 10)
					{
						_state.Cherries = 0;

						if (_state.Lives < _state.MaxLives)
						{
							_state.Lives++;
							track = "extra-life";
							message = "1UP!";
						}
						else
						{
							_state.Score += 5000;
							message = "5000";
						}
					}

					SoundSystem.Play(World, track);

					// Floating reward text: drifts up and expires after ~2s.
					if (message != null)
					{
						World.Create(
							new Text()
							{
								Alignment = Alignment.Center,
								Color = Pico8Color.Color7,
								Content = message,
								Font = "pico-8",
							},
							new Direction(0, -1),
							new Transform(
								position: new Vector2(pickupPosition.X + 4, pickupPosition.Y + 4),
								rotation: 0f,
								scale: Vector2.One
							),
							new Velocity(0, 15),
							new TimeToLive(2f)
						);
					}

					// Pink pickup shockwave centred on the cherry.
					World.Create(
						new Shockwave(
							radius: 3,
							targetRadius: 6,
							color: Pico8Color.Color14,
							speed: 30
						),
						new Transform(
							position: new Vector2(
								pickupPosition.X + (pickupSprite.CurrentFrame.Width / 2),
								pickupPosition.Y + (pickupSprite.CurrentFrame.Height / 2)
							),
							rotation: 0f,
							scale: Vector2.One
						)
					);

					World.Destroy(entity);
				}
			);
		}
	}
}
