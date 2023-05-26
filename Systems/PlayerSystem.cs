using System;
using CherryBomb;
using Components;
using EntityFactories;
using Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class PlayerSystem : EntityUpdateSystem
	{
		private ComponentMapper<Direction> _directionManager;
		private ComponentMapper<TagPlayer> _tagPlayerMapper;
		private ComponentMapper<Transform> _transformMapper;
		private readonly Timer _bulletTimer;

		public PlayerSystem() : base(Aspect.All(typeof(Direction), typeof(TagPlayer), typeof(Transform)))
		{
			_bulletTimer = new Timer(0.133f);
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_directionManager = mapperService.GetMapper<Direction>();
			_tagPlayerMapper = mapperService.GetMapper<TagPlayer>();
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Update(GameTime gameTime)
		{
			_bulletTimer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			// Debug.WriteLine($"{(float)gameTime.ElapsedGameTime.TotalSeconds} {_bulletTimer.IsExpired.ToString()}");

			foreach (var entity in ActiveEntities)
			{
				var direction = _directionManager.Get(entity);
				var tagPlayer = _tagPlayerMapper.Get(entity);
				var transform = _transformMapper.Get(entity);

				direction.X = 0;
				direction.Y = 0;

				if (Keyboard.GetState().IsKeyDown(Keys.Left))
				{
					direction.X = -1;
				}
				else if (Keyboard.GetState().IsKeyDown(Keys.Right))
				{
					direction.X = 1;
				}

				if (Keyboard.GetState().IsKeyDown(Keys.Up))
				{
					direction.Y = -1;
				}
				else if (Keyboard.GetState().IsKeyDown(Keys.Down))
				{
					direction.Y = 1;
				}

				if (Keyboard.GetState().IsKeyDown(Keys.X) && _bulletTimer.IsExpired)
				{
					_bulletTimer.Reset();
					var bullet = CreateEntity();

					bullet.Attach(new BoxCollider(6, 8));
					bullet.Attach(new CollisionLayer(CollisionMasks.PlayerProjectile));
					bullet.Attach(new CollisionMask(CollisionMasks.Enemy));
					bullet.Attach(new Direction(0, -1));
					bullet.Attach(new Sprite(new Rectangle(0, 8, 6, 8)));
					bullet.Attach(new TagBullet());
					bullet.Attach(new Transform(transform.Position + new Vector2(-1, -8), 0f, new Vector2(1f, 1f)));
					bullet.Attach(new Velocity(0, 120));

					// var shockwaveEntity = CreateEntity();

					// var shockwaveColor = new Components.Color(Pico8Color.Color9.R, Pico8Color.Color9.G, Pico8Color.Color9.B, Pico8Color.Color9.A);
					// var shockwave = new Shockwave(3, 6, shockwaveColor, 30);
					// var offset = new Vector2(MathF.Floor(shockwave.Radius) + 2, MathF.Floor(shockwave.Radius) + 6);

					// shockwaveEntity.Attach(shockwave);
					// shockwaveEntity.Attach(new Transform(transform.Position - offset, 0f, new Vector2(1f, 1f)));

					// var random = new Random();

					// // Initial flash
					// ExplosionFactory.CreateExplosion(
					// 	createEntityFn: CreateEntity,
					// 	count: 1,
					// 	() => new Direction(
					// 		x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
					// 		y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
					// 	),
					// 	() => new Particle(
					// 		age: 0,
					// 		maxAge: 0,
					// 		color: new XnaColor(Pico8Color.Color7.R, Pico8Color.Color7.G, Pico8Color.Color7.B, Pico8Color.Color7.A),
					// 		isBlue: false,
					// 		radius: 10,
					// 		shape: Components.Shape.Circle,
					// 		spark: false
					// 	),
					// 	() => new Vector2(64, 64),
					// 	() => new Velocity()
					// );

					// // Large particles
					// ExplosionFactory.CreateExplosion(
					// 	createEntityFn: CreateEntity,
					// 	count: 30,
					// 	() => new Direction(
					// 		x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
					// 		y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
					// 	),
					// 	() => new Particle(
					// 		age: random.NextSingle(0.06f),
					// 		maxAge: 0.266f + random.NextSingle(0.266f),
					// 		color: new XnaColor(Pico8Color.Color7.R, Pico8Color.Color7.G, Pico8Color.Color7.B, Pico8Color.Color7.A),
					// 		isBlue: false,
					// 		radius: 1 + random.NextSingle(4),
					// 		shape: Components.Shape.Circle,
					// 		spark: false
					// 	),
					// 	() => new Vector2(64, 64),
					// 	() => new Velocity(
					// 		x: random.NextSingle() * 50,
					// 		y: random.NextSingle() * 50
					// 	)
					// );

					// // Sparks
					// ExplosionFactory.CreateExplosion(
					// 	createEntityFn: CreateEntity,
					// 	count: 20,
					// 	() => new Direction(
					// 		x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
					// 		y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
					// 	),
					// 	() => new Particle(
					// 		age: random.NextSingle(0.06f),
					// 		maxAge: 0.266f + random.NextSingle(0.266f),
					// 		color: new XnaColor(Pico8Color.Color7.R, Pico8Color.Color7.G, Pico8Color.Color7.B, Pico8Color.Color7.A),
					// 		isBlue: true,
					// 		radius: 1 + random.NextSingle(4),
					// 		shape: Components.Shape.Circle,
					// 		spark: true
					// 	),
					// 	() => new Vector2(64, 64),
					// 	() => new Velocity(
					// 		x: random.NextSingle() * 60,
					// 		y: random.NextSingle() * 60
					// 	)
					// );
				}
			}
		}
	}
}