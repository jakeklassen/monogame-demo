using System.Collections.Generic;
using Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class TextRenderingSystem : EntityDrawSystem
	{
		private readonly SpriteBatch _spriteBatch;

		private readonly OrthographicCamera _camera;

		private readonly Dictionary<string, BitmapFont> _fontCache = new();

		private ComponentMapper<Text> _textMapper;

		private ComponentMapper<Transform> _transformMapper;

		public TextRenderingSystem(GraphicsDevice graphicsDevice, OrthographicCamera camera, Dictionary<string, BitmapFont> fontCache)
			: base(Aspect.All(typeof(Text), typeof(Transform)))
		{
			_camera = camera;
			_fontCache = fontCache;
			_spriteBatch = new SpriteBatch(graphicsDevice);
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_textMapper = mapperService.GetMapper<Text>();
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Draw(GameTime gameTime)
		{
			_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

			foreach (var entityId in ActiveEntities)
			{
				var textEntity = GetEntity(entityId);
				var text = _textMapper.Get(entityId);
				var textColor = new XnaColor(text.Color.R, text.Color.G, text.Color.B, text.Color.A);
				var transform = _transformMapper.Get(entityId);

				var blink = textEntity.Get<Blink>();
				textColor = blink != null
					? blink.CurrentColor.XnaColor
					: textColor;

				var font = _fontCache[text.Font] ?? throw new System.Exception($"Font '{text.Font}' not found in cache.");

				var contentMeasurement = font.MeasureString(text.Content);

				_spriteBatch.DrawString(
					font,
					text.Content,
					transform.Position,
					textColor,
					0f,
					text.Alignment == Alignment.Center
						? new Vector2(contentMeasurement.Width / 2, 0)
						: Vector2.Zero,
					1f,
					SpriteEffects.None,
					0f
				);
			}

			_spriteBatch.End();
		}
	}
}