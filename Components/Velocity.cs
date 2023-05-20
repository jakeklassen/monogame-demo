public class Velocity
{
  public float X { get; set; }
  public float Y { get; set; }

  public Velocity()
  {
    X = 0;
    Y = 0;
  }

  public Velocity(float x, float y)
  {
    X = x;
    Y = y;
  }

  public Velocity(Velocity velocity)
  {
    X = velocity.X;
    Y = velocity.Y;
  }
}