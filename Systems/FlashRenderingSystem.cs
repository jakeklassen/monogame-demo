using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class FlashRenderingSystem : SystemBase<GameTime>
	{
		private readonly QueryDescription _spriteQuery = new QueryDescription().WithAll<
			Flash,
			Sprite,
			Transform
		>();
		private readonly SpriteBatch _spriteBatch;

		private readonly OrthographicCamera _camera;

		private readonly Texture2D _spriteSheetTexture;

		public FlashRenderingSystem(
			World world,
			GraphicsDevice graphicsDevice,
			OrthographicCamera camera,
			Texture2D spriteSheetTexture
		)
			: base(world)
		{
			_camera = camera;
			_spriteBatch = new SpriteBatch(graphicsDevice);
			_spriteSheetTexture = spriteSheetTexture;
		}

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

			var entities = new List<Entity>();
			World.GetEntities(in _spriteQuery, entities);

			entities.Sort((a, b) => a.Id.CompareTo(b.Id));

			foreach (var entity in entities)
			{
				var flash = World.Get<Flash>(entity);
				var sprite = World.Get<Sprite>(entity);
				var transform = World.Get<Transform>(entity);

				flash.Duration -= (float)gameTime.ElapsedGameTime.TotalSeconds;
				flash.Alpha *= (float)gameTime.ElapsedGameTime.TotalSeconds;

				if (flash.Duration <= 0 || flash.Alpha <= 0)
				{
					World.Remove<Flash>(entity);
				}

				var texture = new Texture2D(
					_spriteBatch.GraphicsDevice,
					sprite.CurrentFrame.Width,
					sprite.CurrentFrame.Height
				);

				var colors = new XnaColor[sprite.CurrentFrame.Width * sprite.CurrentFrame.Height];
				_spriteSheetTexture.GetData(
					0,
					sprite.CurrentFrame,
					colors,
					0,
					sprite.CurrentFrame.Width * sprite.CurrentFrame.Height
				);

				// Replace every non-transparent color with flash color
				for (int i = 0; i < colors.Length; i++)
				{
					if (colors[i].A > 0)
					{
						colors[i] = new XnaColor(flash.Color.R, flash.Color.G, flash.Color.B, flash.Alpha);
					}
				}

				texture.SetData(colors);

				_spriteBatch.Draw(
					texture,
					transform.Position,
					new Rectangle(0, 0, sprite.CurrentFrame.Width, sprite.CurrentFrame.Height),
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
