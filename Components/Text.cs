using CherryBomb;

namespace Components
{
	public enum Alignment
	{
		Left,
		Center,
		Right
	}

	public class Text
	{
		public Alignment Alignment { get; set; } = Alignment.Center;
		public Color Color { get; set; } = Pico8Color.Color7;
		public string Content { get; set; } = null;
		public string Font { get; set; } = null;
	}
}