using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace monogame_demo;

public class SimpleFps
{
  private double frames = 0;
  private double updates = 0;
  private double elapsed = 0;
  private double last = 0;
  private double now = 0;
  public double msgFrequency = 1.0f;
  public string msg = "";

  public void Update(GameTime gameTime)
  {
    now = gameTime.TotalGameTime.TotalSeconds;
    elapsed = (double)(now - last);

    if (elapsed > msgFrequency)
    {
      msg = "Fps: " + (frames / elapsed).ToString() + "\n\nElapsed time: " + elapsed.ToString() + "\n\nUpdates: " + updates.ToString() + "\n\nFrames: " + frames.ToString();
      elapsed = 0;
      frames = 0;
      updates = 0;
      last = now;
    }

    updates++;
  }

  public void DrawFps(SpriteBatch spriteBatch, BitmapFont font, Vector2 fpsDisplayPosition, Color fpsTextColor)
  {
    spriteBatch.DrawString(font, msg, fpsDisplayPosition, fpsTextColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    frames++;
  }
}