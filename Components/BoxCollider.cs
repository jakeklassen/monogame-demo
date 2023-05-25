using Microsoft.Xna.Framework;

public class BoxCollider
{
  public int Width { get; private set; } = 0;
  public int Height { get; private set; } = 0;
  public Vector2 Offset { get; private set; } = Vector2.Zero;

  public BoxCollider(int width, int height, Vector2 offset = default(Vector2))
  {
    Width = width;
    Height = height;
    Offset = offset;
  }
}