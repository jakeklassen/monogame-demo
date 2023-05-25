using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.BitmapFonts;

namespace CherryBomb;

public class SimpleFps
{
	private double _frames = 0;
	private double _updates = 0;
	private double _elapsed = 0;
	private double _last = 0;
	private double _now = 0;
	public double MessageFrequency = 1.0f;
	public string Message = "";

	public void Update(GameTime gameTime)
	{
		_now = gameTime.TotalGameTime.TotalSeconds;
		_elapsed = (double)(_now - _last);

		if (_elapsed > MessageFrequency)
		{
			Message = "Fps: " + (_frames / _elapsed).ToString() + "\n\nElapsed time: " + _elapsed.ToString() + "\n\nUpdates: " + _updates.ToString() + "\n\nFrames: " + _frames.ToString();
			_elapsed = 0;
			_frames = 0;
			_updates = 0;
			_last = _now;
		}

		_updates++;
	}

	public void DrawFps(SpriteBatch spriteBatch, BitmapFont font, Vector2 fpsDisplayPosition, Color fpsTextColor)
	{
		spriteBatch.DrawString(font, Message, fpsDisplayPosition, fpsTextColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		_frames++;
	}
}