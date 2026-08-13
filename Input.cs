using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SpaceDrift
{
	// A snapshot of the high-level game actions for one frame. Ported from
	// space-drift/input.ts: the sim reads actions, never raw devices. Sampled once
	// per frame and reused for every fixed step that frame (the device state is
	// constant across the accumulator loop, matching the source).
	public readonly struct InputState
	{
		public bool RotateLeft { get; init; }
		public bool RotateRight { get; init; }
		public bool Thrust { get; init; }
		public bool Brake { get; init; }
		public bool Boost { get; init; }
		public bool Shoot { get; init; }
		public bool Homing { get; init; }

		// Absolute target heading in DEGREES from the left stick (0 = up,
		// clockwise-positive), or null when centred / no pad — then the digital
		// rotate keys apply.
		public float? SteerHeading { get; init; }
	}

	public static class Input
	{
		// Left-stick magnitude past this counts as a digital press.
		private const float StickDeadzone = 0.4f;
		private const float TriggerThreshold = 0.5f;

		public static InputState Sample()
		{
			var k = Keyboard.GetState();
			// Read the raw stick (no XNA deadzone) so our own 0.4 deadzone matches
			// the source exactly.
			var p = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.None);

			// XNA left stick: X = right+, Y = up+.
			var stick = p.ThumbSticks.Left;
			bool stickPushed =
				p.IsConnected
				&& (stick.X * stick.X + stick.Y * stick.Y) > StickDeadzone * StickDeadzone;

			// Absolute stick steering. Source: atan2(s.x, -s.y) where up is -y in
			// the standard mapping. XNA's Y is +up, so -s.y == stick.Y here, giving
			// atan2(stick.X, stick.Y). Null in the deadzone so the keys/D-pad apply.
			float? steerHeading = null;
			if (stickPushed)
			{
				steerHeading = MathF.Atan2(stick.X, stick.Y) * 180f / MathF.PI;
			}

			bool dpadLeft = p.DPad.Left == ButtonState.Pressed;
			bool dpadRight = p.DPad.Right == ButtonState.Pressed;

			return new InputState
			{
				RotateLeft = k.IsKeyDown(Keys.Left) || k.IsKeyDown(Keys.A) || dpadLeft,
				RotateRight = k.IsKeyDown(Keys.Right) || k.IsKeyDown(Keys.D) || dpadRight,
				// Any stick push is "gas": the ship thrusts along its nose.
				Thrust = k.IsKeyDown(Keys.Up) || k.IsKeyDown(Keys.W) || stickPushed,
				Brake =
					k.IsKeyDown(Keys.Down)
					|| k.IsKeyDown(Keys.S)
					|| p.Triggers.Left > TriggerThreshold,
				Boost = k.IsKeyDown(Keys.Z) || p.Triggers.Right > TriggerThreshold,
				Shoot = k.IsKeyDown(Keys.Space) || p.Buttons.A == ButtonState.Pressed,
				Homing = k.IsKeyDown(Keys.X) || p.Buttons.B == ButtonState.Pressed,
				SteerHeading = steerHeading,
			};
		}
	}
}
