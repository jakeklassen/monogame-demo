using CherryBomb;
using Components;
using Lib.Tweening;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class NextWaveEventSystem : EntityProcessingSystem
	{
		private readonly Game1 _game;
		private readonly Tweener _tweener;

		public NextWaveEventSystem(Game1 game, Tweener tweener) : base(Aspect.All(typeof(EventNextWave)))
		{
			_game = game;
			_tweener = tweener;
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
		}

		public override void Process(GameTime gameTime, int entityId)
		{
			_game.State.WaveReady = false;
			_game.State.Wave++;

			_game.Config.Waves.TryGetValue(_game.State.Wave, out var wave);

			if (wave == null)
			{
				DestroyEntity(entityId);

				return;
			}

			var textEntity = CreateEntity();
			var text = new Text()
			{
				Alignment = Alignment.Center,
				Color = Pico8Color.Color6,
				Content = _game.State.Wave < _game.State.MaxWaves
				? $"Wave {_game.State.Wave} of {_game.State.MaxWaves}"
				: "Final Wave!",
				Font = "pico-8",
			};

			textEntity.Attach(text);
			textEntity.Attach(new Blink(
				colors: new[] { Pico8Color.Color5, Pico8Color.Color6, Pico8Color.Color7 },
				colorSequence: new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 0 },
				durationSeconds: 0.5f
			));
			textEntity.Attach(new Transform(
				position: new Vector2(
					x: Game1.TargetWidth / 2,
					y: 40
				),
				rotation: 0f,
				scale: Vector2.One
			));
			textEntity.Attach(new TimeToLive(2.6f));

			if (_game.State.Wave > 1)
			{
				// TODO: play wave-complete sound
			}

			// TODO: Refactor this!
			for (var y = 0; y < wave.Enemies.Length; y++)
			{
				var row = wave.Enemies[y];

				for (var x = 0; x < row.Length; x++)
				{
					var enemyType = row[x];

					if (enemyType == 0)
					{
						continue;
					}

					var destinationX = ((x + 1) * 12) - 6;
					var destinationY = 4 + ((y + 1) * 12);

					var spawnPosition = new Vector2(
						x: (destinationX * 1.25f) - 16,
						y: destinationY - 66
					);

					var enemyDestination = new Vector2(
						x: destinationX,
						y: destinationY
					);

					var transform = new Transform(
						position: spawnPosition,
						rotation: 0f,
						scale: Vector2.One
					);

					var tweenDuration = 0.4f;

					var enemy = CreateEntity();
					enemy.Attach(new BoxCollider(8, 8));
					enemy.Attach(new CollisionLayer(CollisionMasks.Enemy));
					enemy.Attach(new CollisionMask(CollisionMasks.Player | CollisionMasks.PlayerProjectile));
					enemy.Attach(new EnemyState() { Value = EnemyStateType.Flyin });
					enemy.Attach(new Invulnerable() { Duration = tweenDuration });
					enemy.Attach(new Sprite(new Rectangle(40, 8, 8, 8)));
					enemy.Attach(SpriteAnimation.Factory(
						animationDetails: new AnimationDetails()
						{
							Name = "green-alien-idle",
							SourceX = 40,
							SourceY = 8,
							FrameHeight = 8,
							FrameWidth = 8,
							Width = 32,
							Height = 8,
						},
						durationSeconds: 0.4f,
						loop: true
					));
					enemy.Attach(new TagEnemy());
					enemy.Attach(transform);

					_tweener.TweenTo(
						target: transform,
						expression: transform => transform.Position,
						toValue: enemyDestination,
						duration: tweenDuration,
						delay: 2.6f + (x * 90 / 1000f)
					).Easing(EasingFunctions.Linear);
				}
			}

			DestroyEntity(entityId);
		}
	}
}