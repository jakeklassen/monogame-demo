using System;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Drives the horizontal weave of attacking enemies. Velocity/MovementSystem owns
	// the downward (Y) motion; this system owns X so the two never fight. Ports the
	// edge-aware position.x yoyo tweens from switch-enemy-to-attach-mode.ts as a
	// sinusoidal oscillation around Sway.CenterX.
	public class SwaySystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			Sway,
			Transform
		>();

		public override void Update(in GameTime gameTime)
		{
			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			World.Query(
				in _query,
				(ref Sway sway, ref Transform transform) =>
				{
					sway.Elapsed += dt;

					var phase = sway.Direction * MathHelper.TwoPi * (sway.Elapsed / sway.Period);
					var x = sway.CenterX + (sway.Amplitude * (float)Math.Sin(phase));

					transform.Position = new Vector2(x, transform.Position.Y);
				}
			);
		}
	}
}
