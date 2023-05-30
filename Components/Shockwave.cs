namespace Components
{
	public class Shockwave
	{
		public float Radius { get; set; }
		public float TargetRadius { get; set; }
		public Color Color { get; set; }
		public float Speed { get; set; }

		public Shockwave(float radius, float targetRadius, Color color, float speed)
		{
			Radius = radius;
			TargetRadius = targetRadius;
			Color = color;
			Speed = speed;
		}
	}
}
