using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;

namespace CherryBomb.Systems
{
	// Charge while the homing button is held; on release fire a homing volley at
	// the locked target, then emit that volley staggered over the next steps.
	// Ported EXACTLY from space-drift/sim.ts homingSystem (+ helpers). The lock-on
	// picks the nearest on-screen enemy; charge tiers award 3 / 5 / 8 missiles that
	// bloom centre-out in a wide fan. LockTarget/Charging feed the render reticle.
	public sealed class HomingSystem(World world, Entity ship)
	{
		private const float DegToRad = MathF.PI / 180f;

		private readonly World _world = world;
		private readonly Entity _ship = ship;
		private readonly QueryDescription _enemyQuery = new QueryDescription().WithAll<
			Enemy,
			Transform
		>();
		private readonly List<Entity> _enemies = [];

		private float _charge;
		private bool _held;
		private Entity? _latched; // target latched while charging (survives brief loss)
		private Entity? _lock;

		// Pending staggered volley emitted over subsequent steps.
		private int _volleyRemaining;
		private int _volleyTotal;
		private float _volleyTimer;
		private Entity? _volleyTarget;

		// For the render reticle.
		public Entity? LockTarget => _lock;
		public bool Charging => _held;

		public void Update(float dt, in InputState input)
		{
			// Recompute the lock every step so the reticle tracks whatever we face.
			_lock = FindLockTarget();

			bool held = input.Homing;
			if (held)
			{
				_charge = MathF.Min(Constants.HomingChargeMax, _charge + dt);
				// Latch the most recent lock so a momentary loss on release still fires.
				if (_lock is Entity lk)
					_latched = lk;
			}
			else if (_held)
			{
				// Released: commit a volley if we charged enough and had a target.
				int count = ChargeToCount(_charge);
				Entity? target = TargetIsLive(_latched) ? _latched : _lock;
				if (count > 0 && target is Entity t)
				{
					_volleyRemaining = count;
					_volleyTotal = count;
					_volleyTimer = 0f;
					_volleyTarget = t;
				}
				_charge = 0f;
				_latched = null;
			}
			_held = held;

			// Emit the queued volley: the centre (or innermost pair) first, then each
			// symmetric pair launched together on the next tick, so the spread blooms
			// outward like a bulb rather than sweeping across from one side.
			if (_volleyRemaining > 0 && _volleyTarget is Entity vt && _world.IsAlive(vt))
			{
				_volleyTimer -= dt;
				while (_volleyRemaining > 0 && _volleyTimer <= 0f)
				{
					int i0 = _volleyTotal - _volleyRemaining;
					float offset0 = FanOffsetDeg(i0, _volleyTotal);
					LaunchHomingMissile(vt, i0);
					_volleyRemaining -= 1;
					// Fire the mirror partner in the same tick (skips the lone centre).
					if (
						_volleyRemaining > 0
						&& MathF.Abs(
							FanOffsetDeg(_volleyTotal - _volleyRemaining, _volleyTotal) + offset0
						) < 1e-6f
					)
					{
						LaunchHomingMissile(vt, _volleyTotal - _volleyRemaining);
						_volleyRemaining -= 1;
					}
					_volleyTimer += Constants.HomingStagger;
				}
				if (_volleyRemaining <= 0)
					_volleyTarget = null;
			}
		}

		// Projectiles awarded for a charge held t seconds (0 below the 1s floor).
		private static int ChargeToCount(float t) =>
			t >= 3f ? 8
			: t >= 2f ? 5
			: t >= 1f ? 3
			: 0;

		private bool TargetIsLive(Entity? target)
		{
			if (target is not Entity t)
				return false;
			if (!_world.IsAlive(t) || !_world.Has<Transform>(t))
				return false;
			if (_world.Has<Enemy>(t) && _world.Get<Enemy>(t).RespawnTimer > 0f)
				return false;
			return true;
		}

		// The nearest live enemy currently on screen, else null.
		private Entity? FindLockTarget()
		{
			var shipPos = _world.Get<Transform>(_ship).Position;
			float halfW = Constants.GameWidth / 2f + Constants.HomingLockMargin;
			float halfH = Constants.GameHeight / 2f + Constants.HomingLockMargin;

			_enemies.Clear();
			_world.Query(
				in _enemyQuery,
				(Entity e, ref Enemy en) =>
				{
					if (en.RespawnTimer <= 0f)
						_enemies.Add(e);
				}
			);

			Entity? best = null;
			float bestDistSq = float.PositiveInfinity;
			foreach (var ee in _enemies)
			{
				ref var etf = ref _world.Get<Transform>(ee);
				float dx = etf.Position.X - shipPos.X;
				float dy = etf.Position.Y - shipPos.Y;
				if (MathF.Abs(dx) > halfW || MathF.Abs(dy) > halfH)
					continue; // off-screen
				float distSq = dx * dx + dy * dy;
				if (distSq < bestDistSq)
				{
					bestDistSq = distSq;
					best = ee;
				}
			}
			return best;
		}

		// Fan offset (deg) for the i-th missile launched in a volley of `total`.
		// Slots are evenly spaced across the fan, but launched CENTRE-OUT (centre
		// first, then symmetric pairs fanning outward) so the volley blooms like a
		// bulb instead of wiping across from one side to the other.
		private static float FanOffsetDeg(int i, int total)
		{
			if (total <= 1)
				return 0f;
			Span<float> fracs = stackalloc float[total];
			for (int k = 0; k < total; k++)
				fracs[k] = (float)k / (total - 1) - 0.5f;
			// Sort by |value| asc, then value asc (matches JS a.sort comparator).
			for (int a = 1; a < total; a++)
			{
				float v = fracs[a];
				int b = a - 1;
				while (
					b >= 0
					&& (
						MathF.Abs(fracs[b]) > MathF.Abs(v)
						|| (MathF.Abs(fracs[b]) == MathF.Abs(v) && fracs[b] > v)
					)
				)
				{
					fracs[b + 1] = fracs[b];
					b--;
				}
				fracs[b + 1] = v;
			}
			return fracs[i] * Constants.HomingSpreadDeg;
		}

		private void LaunchHomingMissile(Entity target, int index)
		{
			// Copy the ship transform by value: CreateHomingBullet is structural.
			var shipTf = _world.Get<Transform>(_ship);
			float spread = FanOffsetDeg(index, _volleyTotal);
			float angle = (shipTf.Rotation + spread) * DegToRad;
			float hx = MathF.Sin(angle);
			float hy = -MathF.Cos(angle);
			Factories.CreateHomingBullet(
				_world,
				shipTf.Position.X + hx * Constants.MuzzleOffset,
				shipTf.Position.Y + hy * Constants.MuzzleOffset,
				shipTf.Rotation + spread,
				// Constant cruise speed (not ship-relative) keeps the turn radius — and
				// so the seeking — predictable regardless of how fast the ship moves.
				hx * Constants.HomingSpeed,
				hy * Constants.HomingSpeed,
				target
			);
		}
	}
}
