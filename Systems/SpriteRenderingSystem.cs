using System;
using Arch.Core;
using Arch.Core.Extensions;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	public class SpriteRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera,
		Texture2D spriteSheetTexture
	) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _spriteQuery = new QueryDescription()
			.WithAll<Sprite, Transform>()
			.WithNone<Flash>();
		private readonly SpriteBatch _spriteBatch = spriteBatch;

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

			var entities = new Entity[World.CountEntities(in _spriteQuery)];
			World.GetEntities(in _spriteQuery, entities, 0);

			Array.Sort(entities, CompareDrawOrder);

			foreach (var entity in entities)
			{
				var sprite = World.Get<Sprite>(entity);
				var transform = World.Get<Transform>(entity);

				_spriteBatch.Draw(
					_spriteSheetTexture,
					transform.Position,
					sprite.CurrentFrame,
					XnaColor.White * sprite.Opacity,
					transform.Rotation,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					0f
				);
			}

			_spriteBatch.End();
		}

		// Draw back-to-front by explicit Layer, falling back to entity id so order
		// stays stable for the common case where no Layer is set.
		private int CompareDrawOrder(Entity a, Entity b)
		{
			var layerA = World.Has<Layer>(a) ? World.Get<Layer>(a).Value : 0;
			var layerB = World.Has<Layer>(b) ? World.Get<Layer>(b).Value : 0;

			return layerA != layerB ? layerA.CompareTo(layerB) : a.Id.CompareTo(b.Id);
		}
	}
}
