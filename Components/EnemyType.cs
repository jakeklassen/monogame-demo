namespace CherryBomb.Components
{
	// The Config enemy-type id (1-5) an enemy was spawned from. Lets death handling
	// look the enemy's score (and, later, AI behaviour) back up from Config. Stored on
	// the entity at spawn time by NextWaveEventSystem.
	public class EnemyType(int value)
	{
		public int Value { get; set; } = value;
	}
}
