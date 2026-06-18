using System;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace CherryBomb.Systems
{
	// Consumes EventTriggerCameraShake events and jitters the shared camera's
	// position by strength*(rand-0.5) each frame for the event's duration, then
	// snaps it back to its base position. Because every world/HUD rendering system
	// draws through Camera.GetViewMatrix(), the whole frame shakes together and the
	// offset is non-accumulating (reset to base each frame). Source: camera-shake-system.ts.
	public class CameraShakeSystem(World world, OrthographicCamera camera)
		: SystemBase<GameTime>(world)
	{
		private readonly OrthographicCamera _camera = camera;
		private readonly Random _random = new();

		// The camera's resting position for the 128x128 logical view (origin).
		private static readonly Vector2 BasePosition = Vector2.Zero;

		private readonly QueryDescription _query =
			new QueryDescription().WithAll<EventTriggerCameraShake>();

		public override void Update(in GameTime gameTime)
		{
			var dtMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

			World.Query(
				in _query,
				(Entity entity, ref EventTriggerCameraShake shake) =>
				{
					shake.DurationMs -= dtMs;

					if (shake.DurationMs > 0)
					{
						_camera.Position = new Vector2(
							BasePosition.X
								+ (shake.Strength * ((float)_random.NextDouble() - 0.5f)),
							BasePosition.Y + (shake.Strength * ((float)_random.NextDouble() - 0.5f))
						);
					}
					else
					{
						_camera.Position = BasePosition;

						World.Destroy(entity);
					}
				}
			);
		}
	}
}
