using Arch.Core;

namespace CherryBomb.Components
{
	// Emitted by CollisionSystem when the player overlaps a pickup. Mirrors the
	// player-enemy / player-projectile-enemy event pattern: a short-lived event
	// entity that PlayerPickupCollisionEventSystem consumes and then destroys.
	public struct EventPlayerPickupCollision(Entity playerEntity, Entity pickupEntity)
	{
		public Entity PlayerEntity { get; set; } = playerEntity;
		public Entity PickupEntity { get; set; } = pickupEntity;
	}
}
