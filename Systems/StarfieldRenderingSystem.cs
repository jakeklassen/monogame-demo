using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	public class StarfieldRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera
	) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _starsToDraw = new QueryDescription().WithAll<
			Star,
			Transform
		>();
		private readonly SpriteBatch _spriteBatch = spriteBatch;

		private readonly OrthographicCamera _camera = camera;

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
				in _starsToDraw,
				(ref Star star, ref Transform transform) =>
				{
					var rectangle = new RectangleF(
						transform.Position.X,
						transform.Position.Y,
						1,
						1
					);
					var color = new XnaColor(
						star.Color.R,
						star.Color.G,
						star.Color.B,
						star.Color.A
					);

					_spriteBatch.DrawRectangle(rectangle, color);
				}
			);

			_spriteBatch.End();
		}
	}
}
