namespace Components
{
	public class Shockwave(float radius, float targetRadius, Color color, float speed)
	{
		public float Radius { get; set; } = radius;
		public float TargetRadius { get; set; } = targetRadius;
		public Color Color { get; set; } = color;
		public float Speed { get; set; } = speed;
	}
}
