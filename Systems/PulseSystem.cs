using Arch.Core;
using CherryBomb.Components;

namespace CherryBomb.Systems
{
	// Cosmetic phase advance (twinkle / soft pulsing). Ported from
	// space-drift/sim.ts pulseSystem — runs on the REAL frame delta, not the
	// fixed step.
	public sealed class PulseSystem(World world)
	{
		private readonly World _world = world;
		private readonly QueryDescription _query = new QueryDescription().WithAll<Pulse>();

		public void Update(float dt)
		{
			_world.Query(
				in _query,
				(ref Pulse pulse) =>
				{
					pulse.Time += pulse.Speed * dt;
				}
			);
		}
	}
}
