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

					Tween lastTween = null;

					// TODO: Refactor this!
					for (var y = 0; y < wave.Enemies.Length; y++)
					{
						var row = wave.Enemies[y];

						for (var x = 0; x < row.Length; x++)
						{
							var enemyType = row[x];

							// Empty
							if (enemyType == 0)
							{
								continue;
							}

							var enemy =
								_game.Config.Entities.Enemies.GetEnemyConfig(enemyType)
								?? throw new System.Exception($"Enemy type {enemyType} not found!");

							var destinationX = ((x + 1) * 12) - 6;
							var destinationY = 4 + ((y + 1) * 12);

							var spawnPosition = new Vector2(x: (destinationX * 1.25f) - 16, y: destinationY - 66);
							var enemyDestination = new Vector2(x: destinationX, y: destinationY);

							if (enemyType == 5)
							{
								spawnPosition = new Vector2(x: 48, y: -48);
								enemyDestination = new Vector2(x: 48, y: 25);
							}

							var transform = new Transform(
								position: spawnPosition,
								rotation: 0f,
								scale: Vector2.One
							);

							var tweenDuration = 0.4f;
							var tweenDelay = 2.6f + (x * 90 / 1000f);

							SpriteSheet.SpriteData spriteData = enemyType switch
							{
								1 => SpriteSheet.Enemies.GreenAlien,
								2 => SpriteSheet.Enemies.RedFlameGuy,
								3 => SpriteSheet.Enemies.SpinningShip,
								4 => SpriteSheet.Enemies.YellowShip,
								5 => SpriteSheet.Enemies.Boss,
								_ => throw new System.Exception($"Enemy type {enemyType} not found!"),
							};

							var idleAnimationData = spriteData.Animations.TryGetValue(
								"Idle",
								out var animationData
							)
								? animationData
								: throw new System.Exception(
									$"Enemy type {enemyType} does not have an idle animation!"
								);

							var enemyEntity = this.World.Create();
							World.Add(enemyEntity, spriteData.BoxCollider);
							World.Add(enemyEntity, new CollisionLayer(CollisionMasks.Enemy));
							World.Add(
								enemyEntity,
								new CollisionMask(CollisionMasks.Player | CollisionMasks.PlayerProjectile)
							);
							World.Add(enemyEntity, new EnemyState() { Value = EnemyStateType.Flyin });
							World.Add(enemyEntity, new Health(enemy.StartingHealth));
							World.Add(enemyEntity, new Invulnerable() { Duration = tweenDuration + tweenDelay });
							World.Add(enemyEntity, new Sprite(spriteData.Frame));
							World.Add(
								enemyEntity,
								SpriteAnimation.Factory(
									animationDetails: new AnimationDetails()
									{
										Name = "idle",
										SourceX = idleAnimationData.SourceX,
										SourceY = idleAnimationData.SourceY,
										FrameHeight = idleAnimationData.FrameHeight,
										FrameWidth = idleAnimationData.FrameWidth,
										Width = idleAnimationData.Width,
										Height = idleAnimationData.Height,
									},
									durationSeconds: 0.4f,
									loop: true
								)
							);
							World.Add(enemyEntity, new TagEnemy());
							World.Add(enemyEntity, transform);

							lastTween = _tweener
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

					lastTween.OnEnd((action) => _game.State.WaveReady = true);

					World.Destroy(entity);
				}
			);
		}
	}
}
