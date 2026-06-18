using Arch.Core;

namespace CherryBomb.Components
{
	// Emitted by CollisionSystem when a player projectile overlaps the boss.
	// Carries the projectile, the boss, and the projectile's damage so the
	// boss-specific handler can apply the hurt flash and subtract health without
	// the generic enemy-death path destroying the boss.
	// Source: eventPlayerProjectileBossCollision.
	public struct EventPlayerProjectileBossCollision(
		Entity projectileEntity,
		Entity bossEntity,
		int damage
	)
	{
		public Entity ProjectileEntity { get; set; } = projectileEntity;
		public Entity BossEntity { get; set; } = bossEntity;
		public int Damage { get; set; } = damage;
	}
}
