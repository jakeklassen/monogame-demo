using System;
using System.Collections.Generic;

using CherryBomb.Components;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace CherryBomb.Systems;

public class ParticleRenderingSystem : EntityDrawSystem
{

	private readonly SpriteBatch _spriteBatch;

	private readonly OrthographicCamera _camera;
	private readonly Dictionary<string, Texture2D> _textureCache;

	private ComponentMapper<Particle> _particleMapper;

	private ComponentMapper<Transform> _transformMapper;

	public ParticleRenderingSystem(GraphicsDevice graphicsDevice, OrthographicCamera camera, Dictionary<string, Texture2D> textureCache) : base(Aspect.All(typeof(Particle), typeof(Transform)))
	{
		_camera = camera;
		_spriteBatch = new SpriteBatch(graphicsDevice);
		_textureCache = textureCache;
	}

	public override void Initialize(IComponentMapperService mapperService)
	{
		_particleMapper = mapperService.GetMapper<Particle>();
		_transformMapper = mapperService.GetMapper<Transform>();
	}

	public override void Draw(GameTime gameTime)
	{
		_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

		foreach (var entity in ActiveEntities)
		{
			var particle = _particleMapper.Get(entity);
			var transform = _transformMapper.Get(entity);

			if (Math.Floor(particle.Radius) <= 0)
			{
				continue;
			}

			if (particle.Spark)
			{
				_spriteBatch.DrawRectangle(
					new RectangleF(transform.Position.X, transform.Position.Y, 1, 1),
					new Microsoft.Xna.Framework.Color(Pico8Color.Color7.R, Pico8Color.Color7.G, Pico8Color.Color7.B, Pico8Color.Color7.A)
				);
			}
			else if (particle.Shape == Components.Shape.Circle)
			{


				var texture = _textureCache[$"circfill-{Math.Floor(particle.Radius)}"];

				_spriteBatch.Draw(
				texture,
				transform.Position - new Vector2(MathF.Floor(particle.Radius), MathF.Floor(particle.Radius)),
				particle.Color
			);
			}
		}

		_spriteBatch.End();
	}
}