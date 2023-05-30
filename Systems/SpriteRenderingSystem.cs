using Arch.Core;
using Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class SpriteRenderingSystem : SystemBase<GameTime>
	{
		private readonly QueryDescription _starsToDraw = new QueryDescription().WithAll<Sprite, Transform>();
		private readonly SpriteBatch _spriteBatch;

		private readonly OrthographicCamera _camera;

		private readonly Texture2D _spriteSheetTexture;

		public SpriteRenderingSystem(World world, GraphicsDevice graphicsDevice, OrthographicCamera camera, Texture2D spriteSheetTexture) : base(world)
		{
			_camera = camera;
			_spriteBatch = new SpriteBatch(graphicsDevice);
			_spriteSheetTexture = spriteSheetTexture;
		}

		public override void Update(in GameTime gameTime)
		{
			_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

			World.Query(in _starsToDraw, (ref Sprite sprite, ref Transform transform) =>
			{
				_spriteBatch.Draw(
					_spriteSheetTexture,
					transform.Position,
					sprite.CurrentFrame,
					XnaColor.White,
					transform.Rotation,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					0f
				);
			});

			_spriteBatch.End();
		}
	}
}