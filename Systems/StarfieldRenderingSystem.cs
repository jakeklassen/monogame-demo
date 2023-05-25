using Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class StarfieldRenderingSystem : EntityDrawSystem
	{
		private readonly SpriteBatch _spriteBatch;

		private readonly OrthographicCamera _camera;

		private ComponentMapper<Star> _starMapper;

		private ComponentMapper<Transform> _transformMapper;

		public StarfieldRenderingSystem(GraphicsDevice graphicsDevice, OrthographicCamera camera) : base(Aspect.All(typeof(Star), typeof(Transform)))
		{
			_camera = camera;
			_spriteBatch = new SpriteBatch(graphicsDevice);
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_starMapper = mapperService.GetMapper<Star>();
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Draw(GameTime gameTime)
		{
			_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

			foreach (var entity in ActiveEntities)
			{
				var star = _starMapper.Get(entity);
				var transform = _transformMapper.Get(entity);

				var rectangle = new RectangleF(transform.Position.X, transform.Position.Y, 1, 1);
				var color = new XnaColor(star.Color.R, star.Color.G, star.Color.B, star.Color.A);

				_spriteBatch.DrawRectangle(rectangle, color);
			}

			_spriteBatch.End();
		}
	}
}