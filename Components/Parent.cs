using Arch.Core;

namespace CherryBomb.Components
{
	// Stores a handle to the parent entity. Arch entities are value handles, so
	// we keep the Entity by value (never a managed object reference). Consumers
	// must guard access with World.IsAlive(Entity) because the parent may have
	// been destroyed since this component was set.
	public class Parent(Entity entity)
	{
		public Entity Entity { get; set; } = entity;
	}
}
