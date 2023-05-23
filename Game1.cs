using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Entities;
using MonoGame.Extended.ViewportAdapters;

namespace monogame_demo;

public class Game1 : Game
{
  const int TargetWidth = 128;
  const int TargetHeight = 128;

  private OrthographicCamera _camera;

  GraphicsDeviceManager graphics;
  SpriteBatch spriteBatch;

  SimpleFps fps = new SimpleFps();
  BitmapFont font;

  private Texture2D playerTexture;
  private Texture2D mapTexture;
  private Texture2D spriteSheetTexture;

  private World _world;

  private bool hasToggledVsync = false;
  private bool hasToggledFixedTimeStep = false;

  public Game1()
  {
    graphics = new GraphicsDeviceManager(this);
    Content.RootDirectory = "Content";
    IsMouseVisible = true;

    graphics.PreferredBackBufferWidth = 512;
    graphics.PreferredBackBufferHeight = 512;
    // this is for fullscreen but like 'borderless'
    graphics.HardwareModeSwitch = false;
    graphics.IsFullScreen = false;
    graphics.PreferMultiSampling = false;
    graphics.SynchronizeWithVerticalRetrace = true;
    graphics.ApplyChanges();

    // Disable for a better experience with higher refresh rate monitors
    IsFixedTimeStep = false;
  }

  protected override void Initialize()
  {
    base.Initialize();

    var viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, TargetWidth, TargetHeight);
    _camera = new OrthographicCamera(viewportAdapter);

    _world = new WorldBuilder()
      .AddSystem(new PlayerSystem())
      .AddSystem(new MovementSystem())
      .AddSystem(new StarfieldSystem())
      .AddSystem(new StarfieldRenderingSystem(graphics.GraphicsDevice, _camera))
      .AddSystem(new SpriteRenderingSystem(graphics.GraphicsDevice, _camera, spriteSheetTexture))
      .Build();

    {
      var player = _world.CreateEntity();

      player.Attach(new Direction());
      player.Attach(new Sprite(new Rectangle(16, 0, 8, 8)));
      player.Attach(new TagPlayer());
      player.Attach(
        new Transform(new Vector2(TargetWidth / 2 + playerTexture.Width / 2, TargetHeight / 2 + playerTexture.Height / 2), 0f, Vector2.One)
      );
      player.Attach(new Velocity(60, 60));
    }

    StarFactory.CreateStarfield(_world, TargetWidth, TargetHeight, 100);

    Components.Add(_world);
  }

  protected override void LoadContent()
  {
    spriteBatch = new SpriteBatch(GraphicsDevice);

    font = Content.Load<BitmapFont>("Font/pico-8");
    playerTexture = Content.Load<Texture2D>("Graphics/player-ship");
    mapTexture = Content.Load<Texture2D>("Graphics/map");
    spriteSheetTexture = Content.Load<Texture2D>("Graphics/shmup");
  }

  protected override void UnloadContent()
  {
    base.UnloadContent();

    spriteBatch.Dispose();
  }

  protected override void Update(GameTime gameTime)
  {
    // var now = gameTime.TotalGameTime.TotalSeconds;
    // var elapsed = (float)(now - last);
    var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

    if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
      Exit();

    if (Keyboard.GetState().IsKeyDown(Keys.F) && hasToggledFixedTimeStep == false)
    {
      hasToggledFixedTimeStep = true;
      IsFixedTimeStep = !IsFixedTimeStep;

      graphics.ApplyChanges();
    }

    if (Keyboard.GetState().IsKeyUp(Keys.F) && hasToggledFixedTimeStep)
    {
      hasToggledFixedTimeStep = false;
    }

    if (Keyboard.GetState().IsKeyDown(Keys.V) && hasToggledVsync == false)
    {
      hasToggledVsync = true;
      graphics.SynchronizeWithVerticalRetrace = !graphics.SynchronizeWithVerticalRetrace;

      graphics.ApplyChanges();
    }

    if (Keyboard.GetState().IsKeyUp(Keys.V) && hasToggledVsync)
    {
      hasToggledVsync = false;
    }

    fps.Update(gameTime);

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    float frameRate = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;

    GraphicsDevice.Clear(Color.Black);

    spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

    spriteBatch.DrawString(font, $"Is Fixed TimeStep: {IsFixedTimeStep}", new Vector2(2f, 25f), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    spriteBatch.DrawString(font, $"Vsync: {graphics.SynchronizeWithVerticalRetrace}", new Vector2(2f, 40f), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    fps.DrawFps(spriteBatch, font, new Vector2(2f, 55f), Color.White);

    spriteBatch.DrawString(font, $"Entity #: {_world.EntityCount}", new Vector2(2f, 32f), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

    spriteBatch.End();

    base.Draw(gameTime);
  }
}

