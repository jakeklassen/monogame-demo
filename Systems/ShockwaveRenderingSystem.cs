using System;
using System.Collections.Generic;

using CherryBomb.Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Systems;

public class ShockwaveRenderingSystem : EntityDrawSystem
{

	private readonly SpriteBatch _spriteBatch;

	private readonly OrthographicCamera _camera;
	private readonly Dictionary<string, Texture2D> _textureCache;

	private ComponentMapper<Shockwave> _shockwaveMapper;

	private ComponentMapper<Transform> _transformMapper;

	public ShockwaveRenderingSystem(GraphicsDevice graphicsDevice, OrthographicCamera camera, Dictionary<string, Texture2D> textureCache) : base(Aspect.All(typeof(Shockwave), typeof(Transform)))
	{
		_camera = camera;
		_spriteBatch = new SpriteBatch(graphicsDevice);
		_textureCache = textureCache;
	}

	public override void Initialize(IComponentMapperService mapperService)
	{
		_shockwaveMapper = mapperService.GetMapper<Shockwave>();
		_transformMapper = mapperService.GetMapper<Transform>();
	}

	public override void Draw(GameTime gameTime)
	{
		_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

		foreach (var entity in ActiveEntities)
		{
			var shockwave = _shockwaveMapper.Get(entity);
			var transform = _transformMapper.Get(entity);

			var texture = _textureCache[$"circ-{Math.Round(shockwave.Radius)}"];
			var shockwaveColor = new XnaColor(shockwave.Color.R, shockwave.Color.G, shockwave.Color.B, shockwave.Color.A);

			_spriteBatch.Draw(
				texture,
				transform.Position - new Vector2(MathF.Round(shockwave.Radius), MathF.Round(shockwave.Radius)),
				shockwaveColor
			);
		}

		_spriteBatch.End();
	}
}