namespace Components
{
	public enum Shape
	{
		Circle
	}

	public class Particle
	{
		public float Age { get; set; }
		public float MaxAge { get; set; }
		public Microsoft.Xna.Framework.Color Color { get; set; }
		public bool IsBlue { get; set; } = false;
		public float Radius { get; set; }
		public Shape Shape { get; set; } = Shape.Circle;
		public bool Spark { get; set; } = false;

		public Particle(float age, float maxAge, Microsoft.Xna.Framework.Color color, bool isBlue, float radius, Shape shape, bool spark)
		{
			Age = age;
			MaxAge = maxAge;
			Color = color;
			IsBlue = isBlue;
			Radius = radius;
			Shape = shape;
			Spark = spark;
		}
	}
}