using System;
using Arch.Core;
using SpaceDrift.Components;

namespace SpaceDrift.Systems
{
	// Fire on a tap, and stream at ShootInterval while the shoot button is held.
	// Ported EXACTLY from space-drift/sim.ts shootSystem. Bullets inherit the
	// ship's velocity (so a boosting player can't outrun their own fire) and fire
	// as a double-wide pair offset ±ShotSpread from the nose line.
	public sealed class ShootSystem(World world, Entity ship)
	{
		private const float DegToRad = MathF.PI / 180f;

		private readonly World _world = world;
		private readonly Entity _ship = ship;

		// Countdown between shots; reset to 0 on release so the next tap fires now.
		private float _cooldown;

		public void Update(float dt, in InputState input)
		{
			_cooldown -= dt;
			if (!input.Shoot)
			{
				_cooldown = 0f; // released → next press fires immediately
				return;
			}
			if (_cooldown > 0f)
				return;
			_cooldown = Constants.ShootInterval;

			ref var tf = ref _world.Get<Transform>(_ship);
			ref var vel = ref _world.Get<Velocity>(_ship);

			float rad = tf.Rotation * DegToRad;
			float hx = MathF.Sin(rad);
			float hy = -MathF.Cos(rad);
			float perpX = -hy;
			float perpY = hx;
			float muzzleX = tf.Position.X + hx * Constants.MuzzleOffset;
			float muzzleY = tf.Position.Y + hy * Constants.MuzzleOffset;
			// Inherit the ship's velocity so a boosting player can't outrun the shot.
			float vx = vel.Value.X + hx * Constants.BulletSpeed;
			float vy = vel.Value.Y + hy * Constants.BulletSpeed;

			// Double-wide: two parallel bullets offset left/right of the nose line.
			for (int side = -1; side <= 1; side += 2)
			{
				Factories.CreateBullet(
					_world,
					muzzleX + perpX * Constants.ShotSpread * side,
					muzzleY + perpY * Constants.ShotSpread * side,
					tf.Rotation,
					vx,
					vy
				);
			}
		}
	}
}
