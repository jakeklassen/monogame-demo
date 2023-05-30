using Arch.Core;
using CherryBomb;
using Components;
using Lib.Tweening;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class NextWaveEventSystem : SystemBase<GameTime>
	{
		private readonly Game1 _game;
		private readonly Tweener _tweener;
		private readonly QueryDescription _eventEntities =
			new QueryDescription().WithAll<EventNextWave>();

		public NextWaveEventSystem(World world, Game1 game, Tweener tweener)
			: base(world)
		{
			_game = game;
			_tweener = tweener;
		}

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _eventEntities,
				(in Entity entity, ref EventNextWave nextWaveEvent) =>
				{
					_game.State.WaveReady = false;
					_game.State.Wave++;

					_game.Config.Waves.TryGetValue(_game.State.Wave, out var wave);

					if (wave == null)
					{
						World.Destroy(entity);

						return;
					}

					var textEntity = this.World.Create();
					var text = new Text()
					{
						Alignment = Alignment.Center,
						Color = Pico8Color.Color6,
						Content =
							_game.State.Wave < _game.State.MaxWaves
								? $"Wave {_game.State.Wave} of {_game.State.MaxWaves}"
								: "Final Wave!",
						Font = "pico-8",
					};

					World.Add(textEntity, text);
					World.Add(
						textEntity,
						new Blink(
							colors: new[] { Pico8Color.Color5, Pico8Color.Color6, Pico8Color.Color7 },
							colorSequence: new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 0 },
							durationSeconds: 0.5f
						)
					);
					World.Add(
						textEntity,
						new Transform(
							position: new Vector2(x: Game1.TargetWidth / 2, y: 40),
							rotation: 0f,
							scale: Vector2.One
						)
					);
					World.Add(textEntity, new TimeToLive(2.6f));

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

							var spawnPosition = new Vector2(x: (destinationX * 1.25f) - 16, y: destinationY - 66);

							var enemyDestination = new Vector2(x: destinationX, y: destinationY);

							var transform = new Transform(
								position: spawnPosition,
								rotation: 0f,
								scale: Vector2.One
							);

							var tweenDuration = 0.4f;
							var tweenDelay = 2.6f + (x * 90 / 1000f);

							var enemy = this.World.Create();
							World.Add(enemy, new BoxCollider(8, 8));
							World.Add(enemy, new CollisionLayer(CollisionMasks.Enemy));
							World.Add(
								enemy,
								new CollisionMask(CollisionMasks.Player | CollisionMasks.PlayerProjectile)
							);
							World.Add(enemy, new EnemyState() { Value = EnemyStateType.Flyin });
							World.Add(enemy, new Invulnerable() { Duration = tweenDuration + tweenDelay });
							World.Add(enemy, new Sprite(new Rectangle(40, 8, 8, 8)));
							World.Add(
								enemy,
								SpriteAnimation.Factory(
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
								)
							);
							World.Add(enemy, new TagEnemy());
							World.Add(enemy, transform);

							_tweener
								.TweenTo(
									target: transform,
									expression: transform => transform.Position,
									toValue: enemyDestination,
									duration: tweenDuration,
									delay: tweenDelay
								)
								.Easing(EasingFunctions.Linear);
						}
					}

					World.Destroy(entity);
				}
			);
		}
	}
}
