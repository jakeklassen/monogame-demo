using Arch.Core;
using CherryBomb;
using Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class DebugRenderingSystem(
		World world,
		Game1 game,
		GraphicsDevice graphicsDevice,
		OrthographicCamera camera
		) : SystemBase<GameTime>(world)
	{
		private readonly Game1 _game = game;
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			BoxCollider,
			Transform
		>();
		private readonly SpriteBatch _spriteBatch = new(graphicsDevice);

		private readonly OrthographicCamera _camera = camera;

		public override void Update(in GameTime gameTime)
		{
			if (_game.Config.Debug == false)
			{
				return;
			}

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
				in _query,
				(ref BoxCollider boxCollider, ref Transform transform) =>
				{
					var rectangle = new RectangleF(
						transform.Position.X + boxCollider.Offset.X,
						transform.Position.Y + boxCollider.Offset.Y,
						boxCollider.Width,
						boxCollider.Height
					);
					var color = new XnaColor(
						Pico8Color.Color8.R,
						Pico8Color.Color8.G,
						Pico8Color.Color8.B,
						Pico8Color.Color8.A / 2
					);

					_spriteBatch.DrawRectangle(rectangle, color);
				}
			);

			_spriteBatch.End();
		}
	}
}
