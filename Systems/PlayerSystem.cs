using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

public class PlayerSystem : EntityUpdateSystem
{
  private ComponentMapper<Direction> _directionManager;
  private ComponentMapper<TagPlayer> _tagPlayerMapper;

  public PlayerSystem() : base(Aspect.All(typeof(Direction), typeof(TagPlayer)))
  {
  }

  public override void Initialize(IComponentMapperService mapperService)
  {
    _directionManager = mapperService.GetMapper<Direction>();
    _tagPlayerMapper = mapperService.GetMapper<TagPlayer>();
  }

  public override void Update(GameTime gameTime)
  {
    foreach (var entity in ActiveEntities)
    {
      var direction = _directionManager.Get(entity);
      var tagPlayer = _tagPlayerMapper.Get(entity);

      direction.X = 0;
      direction.Y = 0;

      if (Keyboard.GetState().IsKeyDown(Keys.Left))
      {
        direction.X = -1;
      }
      else if (Keyboard.GetState().IsKeyDown(Keys.Right))
      {
        direction.X = 1;
      }

      if (Keyboard.GetState().IsKeyDown(Keys.Up))
      {
        direction.Y = -1;
      }
      else if (Keyboard.GetState().IsKeyDown(Keys.Down))
      {
        direction.Y = 1;
      }
    }
  }
}