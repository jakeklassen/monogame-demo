using Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class SpriteRenderingSystem : EntityDrawSystem
	{
		private readonly SpriteBatch _spriteBatch;

		private readonly OrthographicCamera _camera;

		private readonly Texture2D _spriteSheetTexture;

		private ComponentMapper<Sprite> _spriteMapper;

		private ComponentMapper<Transform> _transformMapper;

		public SpriteRenderingSystem(GraphicsDevice graphicsDevice, OrthographicCamera camera, Texture2D spriteSheetTexture) : base(Aspect.All(typeof(Sprite), typeof(Transform)))
		{
			_camera = camera;
			_spriteBatch = new SpriteBatch(graphicsDevice);
			_spriteSheetTexture = spriteSheetTexture;
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_spriteMapper = mapperService.GetMapper<Sprite>();
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Draw(GameTime gameTime)
		{
			_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

			foreach (var entity in ActiveEntities)
			{
				var sprite = _spriteMapper.Get(entity);
				var transform = _transformMapper.Get(entity);

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
			}

			_spriteBatch.End();
		}
	}
}