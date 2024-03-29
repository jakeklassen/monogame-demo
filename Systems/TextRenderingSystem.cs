using System.Collections.Generic;
using Arch.Core;

using Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class TextRenderingSystem(
		World world,
		GraphicsDevice graphicsDevice,
		OrthographicCamera camera,
		Dictionary<string, BitmapFont> fontCache
		) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _textEntities = new QueryDescription().WithAll<
			Text,
			Transform
		>();
		private readonly SpriteBatch _spriteBatch = new(graphicsDevice);

		private readonly OrthographicCamera _camera = camera;

		private readonly Dictionary<string, BitmapFont> _fontCache = fontCache;

		public override void Update(in GameTime gameTime)
		{
			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				null,
				SamplerState.PointClamp,
				null,
				null,
				null,
				_camera.GetViewMatrix()
			);

			World.Query(
				in _textEntities,
				(Entity entity, ref Text text, ref Transform transform) =>
				{
					var textColor = new XnaColor(text.Color.R, text.Color.G, text.Color.B, text.Color.A);

					World.TryGet<Blink>(entity, out var blink);

					textColor = blink?.CurrentColor?.XnaColor ?? textColor;

					var font =
						_fontCache[text.Font]
						?? throw new System.Exception($"Font '{text.Font}' not found in cache.");

					var contentMeasurement = font.MeasureString(text.Content);

					_spriteBatch.DrawString(
						font,
						text.Content,
						transform.Position,
						textColor,
						0f,
						text.Alignment == Alignment.Center
							? new Vector2(contentMeasurement.Width / 2, 0)
							: Vector2.Zero,
						1f,
						SpriteEffects.None,
						0f
					);
				}
			);

			_spriteBatch.End();
		}
	}
}
