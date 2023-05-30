using Arch.Core;
using Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class StarfieldRenderingSystem : SystemBase<GameTime>
	{
		private readonly QueryDescription _starsToDraw = new QueryDescription().WithAll<Star, Transform>();
		private readonly SpriteBatch _spriteBatch;

		private readonly OrthographicCamera _camera;

		public StarfieldRenderingSystem(World world, GraphicsDevice graphicsDevice, OrthographicCamera camera) : base(world)
		{
			_camera = camera;
			_spriteBatch = new SpriteBatch(graphicsDevice);
		}

		public override void Update(in GameTime gameTime)
		{
			_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

			World.Query(in _starsToDraw, (ref Star start, ref Transform transform) =>
			{
				var rectangle = new RectangleF(transform.Position.X, transform.Position.Y, 1, 1);
				var color = new XnaColor(start.Color.R, start.Color.G, start.Color.B, start.Color.A);

				_spriteBatch.DrawRectangle(rectangle, color);
			});

			_spriteBatch.End();
		}
	}
}