namespace Components
{
	public class Blink
	{
		public Color CurrentColor { get; set; }
		public Color[] Colors { get; set; }
		public int[] ColorSequence { get; set; }
		public int CurrentColorIndex { get; set; } = 0;
		public float DurationSeconds { get; set; } = 0.5f;
		public float ElapsedSeconds { get; set; } = 0f;
		public float FrameRate { get; private set; } = 0f;

		public Blink(Color[] colors, int[] colorSequence, float durationSeconds)
		{
			CurrentColor = colors[colorSequence[CurrentColorIndex]];
			Colors = colors;
			ColorSequence = colorSequence;
			DurationSeconds = durationSeconds;
			FrameRate = DurationSeconds / ColorSequence.Length;
		}
	}
}
