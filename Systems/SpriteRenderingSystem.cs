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
	public class SpriteRenderingSystem(
		World world,
		GraphicsDevice graphicsDevice,
		OrthographicCamera camera,
		Texture2D spriteSheetTexture
		) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _spriteQuery = new QueryDescription()
			.WithAll<Sprite, Transform>()
			.WithNone<Flash>();
		private readonly SpriteBatch _spriteBatch = new(graphicsDevice);

		private readonly OrthographicCamera _camera = camera;

		private readonly Texture2D _spriteSheetTexture = spriteSheetTexture;

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
				var sprite = World.Get<Sprite>(entity);
				var transform = World.Get<Transform>(entity);

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
