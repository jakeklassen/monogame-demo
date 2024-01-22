namespace Components
{
	public enum Shape
	{
		Circle
	}

	public class Particle(
		float age,
		float maxAge,
		Microsoft.Xna.Framework.Color color,
		bool isBlue,
		float radius,
		Shape shape,
		bool spark
		)
	{
		public float Age { get; set; } = age;
		public float MaxAge { get; set; } = maxAge;
		public Microsoft.Xna.Framework.Color Color { get; set; } = color;
		public bool IsBlue { get; set; } = isBlue;
		public float Radius { get; set; } = radius;
		public Shape Shape { get; set; } = shape;
		public bool Spark { get; set; } = spark;
	}
}
