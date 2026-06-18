namespace CherryBomb.Components
{
	// Optional explicit draw order. Lower layers draw first (further back).
	// Entities without a Layer are treated as layer 0, so draw order falls back
	// to entity id — making layering intentional instead of incidental.
	public struct Layer(int value)
	{
		public int Value { get; set; } = value;
	}
}
