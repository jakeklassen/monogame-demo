namespace CherryBomb.Components
{
	// Marker for the spread-shot's big bullets. They behave like player projectiles
	// (layer PlayerProjectile, mask Enemy) but carry their own tag so the collision
	// system can tell them apart from the standard bullet if needed later.
	public struct TagBigBullet { }
}
