using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	// HUD system: draws MaxLives hearts at the top-left, full or empty by State.Lives.
	// Reads State directly (no entities). 9px horizontal spacing, matching the source.
	public class LivesRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera,
		Texture2D spriteSheetTexture,
		State state
	) : SystemBase<GameTime>(world)
	{
		private readonly SpriteBatch _spriteBatch = spriteBatch;
		private readonly OrthographicCamera _camera = camera;
		private readonly Texture2D _spriteSheetTexture = spriteSheetTexture;
		private readonly State _state = state;

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

			for (var i = 0; i < _state.MaxLives; i++)
			{
				var frame =
					i < _state.Lives ? SpriteSheet.Heart.Full.Frame : SpriteSheet.Heart.Empty.Frame;

				_spriteBatch.Draw(
					_spriteSheetTexture,
					new Vector2((i * 9) + 1, 1),
					frame,
					XnaColor.White
				);
			}

			_spriteBatch.End();
		}
	}
}
