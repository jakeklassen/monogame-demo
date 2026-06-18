using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems
{
	public class FlashRenderingSystem(
		World world,
		SpriteBatch spriteBatch,
		OrthographicCamera camera,
		Texture2D spriteSheetTexture
	) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _spriteQuery = new QueryDescription().WithAll<
			Flash,
			Sprite,
			Transform
		>();
		private readonly SpriteBatch _spriteBatch = spriteBatch;

		private readonly OrthographicCamera _camera = camera;

		private readonly Texture2D _spriteSheetTexture = spriteSheetTexture;

		// White silhouette masks cached per source frame. A flash is then just the
		// mask drawn with a tint, instead of allocating a Texture2D and round-tripping
		// pixel data through the GPU on every frame (which also leaked the textures).
		private readonly Dictionary<Rectangle, Texture2D> _masks = [];

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
				var flash = World.Get<Flash>(entity);
				var sprite = World.Get<Sprite>(entity);
				var transform = World.Get<Transform>(entity);

				flash.Elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
				flash.Alpha = MathHelper.Lerp(255f, 0f, flash.Elapsed / flash.Duration);

				if (flash.Duration <= 0 || flash.Alpha <= 0)
				{
					World.Remove<Flash>(entity);
				}

				var mask = GetMask(sprite.CurrentFrame);
				var tint = new XnaColor(flash.Color.R, flash.Color.G, flash.Color.B, flash.Alpha);

				_spriteBatch.Draw(
					mask,
					transform.Position,
					null,
					tint,
					transform.Rotation,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					0f
				);
			}

			_spriteBatch.End();
		}

		// Builds (once per frame rectangle) a white silhouette of the sprite: every
		// non-transparent source pixel becomes solid white so the flash tint shows
		// through, transparent pixels stay clear. Cached for the screen's lifetime —
		// the set of distinct sprite frames is tiny and bounded.
		private Texture2D GetMask(Rectangle frame)
		{
			if (_masks.TryGetValue(frame, out var cached))
			{
				return cached;
			}

			var pixels = new XnaColor[frame.Width * frame.Height];
			_spriteSheetTexture.GetData(0, frame, pixels, 0, pixels.Length);

			for (var i = 0; i < pixels.Length; i++)
			{
				pixels[i] = pixels[i].A > 0 ? XnaColor.White : XnaColor.Transparent;
			}

			var mask = new Texture2D(_spriteBatch.GraphicsDevice, frame.Width, frame.Height);
			mask.SetData(pixels);
			_masks.Add(frame, mask);

			return mask;
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
