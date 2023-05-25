using System;
using MonoGame.Extended.Entities;
using Vector2 = Microsoft.Xna.Framework.Vector2;

static class StarFactory
{
  public static Entity CreateStar(World world, Vector2 position)
  {
    var randomVelocities = new[] { 60, 30, 20 };
    var random = new Random();

    var entity = world.CreateEntity();
    entity.Attach(new Direction(0, 1));
    entity.Attach(new Transform(position, 0f, new Vector2(1f, 1f)));

    var star = new Star(Pico8Color.Color7);
    var velocity = new Velocity(0f, randomVelocities[random.Next(0, randomVelocities.Length)]);

    // Adjust star color based on velocity
    if (velocity.Y < 30)
    {
      star.Color = Pico8Color.Color1;
    }
    else if (velocity.Y < 60)
    {
      star.Color = Pico8Color.Color13;
    }

    entity.Attach(star);
    entity.Attach(velocity);

    return entity;
  }

  public static void CreateStarfield(World world, int width, int height, int starCount)
  {
    var random = new Random();

    for (var i = 0; i < starCount; i++)
    {
      var x = random.Next(0, width);
      var y = random.Next(0, height);

      CreateStar(world, new Vector2(x, y));
    }
  }
}