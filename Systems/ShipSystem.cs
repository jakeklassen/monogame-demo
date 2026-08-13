using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using SpaceDrift.Components;

namespace SpaceDrift.Systems
{
	// Fixed-step ship simulation. Ported EXACTLY from space-drift/sim.ts
	// (shipSystem + emitThrust) — this is the feel. Operates on the single ship
	// entity, saving Previous for interpolation, steering, thrust/boost/brake,
	// fuel, grip (forward/lateral drag split), speed clamp, and integration.
	public sealed class ShipSystem(World world, Entity ship)
	{
		private const float DegToRad = MathF.PI / 180f;

		private readonly World _world = world;
		private readonly Entity _ship = ship;
		private readonly Random _rng = new();

		// Edge-detect the boost key across fixed steps (for the tap-dash).
		private bool _prevBoost = false;

		public void Update(float dt, in InputState input)
		{
			ref var tf = ref _world.Get<Transform>(_ship);
			ref var prev = ref _world.Get<Previous>(_ship);
			ref var vel = ref _world.Get<Velocity>(_ship);
			ref var st = ref _world.Get<Ship>(_ship);

			prev.Position = tf.Position;
			prev.Rotation = tf.Rotation;

			bool boost = input.Boost;

			// Steering. The analog stick sets an absolute target heading; the nose
			// rotates toward it the short way, capped at the turn rate. Keys/D-pad
			// fall back to fixed-rate left/right rotation.
			float maxTurn = Constants.ShipRotationSpeed * dt;
			if (input.SteerHeading is float target)
			{
				// Shortest signed angle from current heading to the target.
				float diff = target - tf.Rotation;
				diff = ((diff + 180f) % 360f + 360f) % 360f - 180f;
				tf.Rotation += Math.Clamp(diff, -maxTurn, maxTurn);
			}
			else
			{
				if (input.RotateLeft)
					tf.Rotation -= maxTurn;
				if (input.RotateRight)
					tf.Rotation += maxTurn;
			}

			float rad = tf.Rotation * DegToRad;
			float hx = MathF.Sin(rad);
			float hy = -MathF.Cos(rad);

			// Tap-dash: a punchy forward burst on the boost press-edge, with fuel.
			if (boost && !_prevBoost && st.Fuel >= Constants.BoostDashCost)
			{
				vel.Value.X += hx * Constants.BoostDashImpulse;
				vel.Value.Y += hy * Constants.BoostDashImpulse;
				st.Fuel -= Constants.BoostDashCost;
			}

			// Holding boost sustains huge thrust, but only while the tank has fuel.
			bool canBoost = boost && st.Fuel > 0f;

			st.Thrusting = false;
			st.Boosting = false;
			if (input.Thrust || canBoost)
			{
				float power = canBoost ? Constants.ShipBoostThrust : Constants.ShipThrust;
				vel.Value.X += hx * power * dt;
				vel.Value.Y += hy * power * dt;
				st.Thrusting = true;
				st.Boosting = canBoost;
				EmitThrust(tf.Position, vel.Value, hx, hy, canBoost);
			}
			if (input.Brake)
			{
				vel.Value.X -= hx * Constants.ShipThrust * Constants.ShipBrake * dt;
				vel.Value.Y -= hy * Constants.ShipThrust * Constants.ShipBrake * dt;
			}

			// Fuel: boosting drains it; anything else refills it.
			if (canBoost)
			{
				st.Fuel = MathF.Max(0f, st.Fuel - Constants.BoostDrain * dt);
			}
			else
			{
				st.Fuel = MathF.Min(Constants.BoostFuelMax, st.Fuel + Constants.BoostRefill * dt);
			}

			// Grip: split velocity into forward (along the nose) and lateral, drag
			// each separately. Heavy lateral drag makes the ship go where it points.
			float perpX = -hy;
			float perpY = hx;
			float fwd = vel.Value.X * hx + vel.Value.Y * hy;
			float lat = vel.Value.X * perpX + vel.Value.Y * perpY;
			float newFwd = fwd * MathF.Max(0f, 1f - Constants.ShipForwardDrag * dt);
			float newLat = lat * MathF.Max(0f, 1f - Constants.ShipLateralDrag * dt);
			vel.Value.X = hx * newFwd + perpX * newLat;
			vel.Value.Y = hy * newFwd + perpY * newLat;

			float speedSq = vel.Value.X * vel.Value.X + vel.Value.Y * vel.Value.Y;
			if (speedSq > Constants.ShipMaxSpeed * Constants.ShipMaxSpeed)
			{
				float scale = Constants.ShipMaxSpeed / MathF.Sqrt(speedSq);
				vel.Value.X *= scale;
				vel.Value.Y *= scale;
			}

			tf.Position.X += vel.Value.X * dt;
			tf.Position.Y += vel.Value.Y * dt;

			_prevBoost = boost;
		}

		private float RndRange(float min, float max) => min + _rng.NextSingle() * (max - min);

		// Exhaust: a converging cone of short-lived pixels streaming into world
		// space. Ported from emitThrust.
		private void EmitThrust(Vector2 pos, Vector2 shipVel, float hx, float hy, bool boost)
		{
			float nozzleX = pos.X - hx * 5f;
			float nozzleY = pos.Y - hy * 5f;
			float perpX = -hy;
			float perpY = hx;

			int count = boost
				? (_rng.NextSingle() < 0.5f ? 7 : 6)
				: (_rng.NextSingle() < 0.5f ? 4 : 3);

			for (int i = 0; i < count; i++)
			{
				float back = boost ? RndRange(26f, 64f) : RndRange(12f, 30f);
				float band = RndRange(-2f, 2f);
				float converge = -band * RndRange(5f, 9f) + RndRange(-3f, 3f);
				float life = RndRange(0.08f, 0.17f);
				if (_rng.NextSingle() < 0.5f)
					life *= 0.6f;

				SpawnParticle(
					nozzleX + perpX * band,
					nozzleY + perpY * band,
					-hx * back + perpX * converge + shipVel.X * 0.25f,
					-hy * back + perpY * converge + shipVel.Y * 0.25f,
					life,
					ParticleKind.Flame
				);
			}

			if (_rng.NextSingle() < 0.2f)
			{
				float back = RndRange(6f, 15f);
				float band = RndRange(-2f, 2f);
				float converge = -band * RndRange(3f, 6f) + RndRange(-2f, 2f);
				SpawnParticle(
					nozzleX + perpX * band,
					nozzleY + perpY * band,
					-hx * back + perpX * converge + shipVel.X * 0.12f,
					-hy * back + perpY * converge + shipVel.Y * 0.12f,
					RndRange(0.22f, 0.4f),
					ParticleKind.Smoke
				);
			}
		}

		private void SpawnParticle(
			float x,
			float y,
			float vx,
			float vy,
			float maxAge,
			ParticleKind kind
		)
		{
			_world.Create(
				new Transform(new Vector2(x, y), 0f),
				new Velocity(new Vector2(vx, vy)),
				new Particle
				{
					Age = 0f,
					MaxAge = maxAge,
					Kind = kind,
					Size = 1f,
				}
			);
		}
	}
}
