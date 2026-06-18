using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.Lib.Tweening;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	public class NextWaveEventSystem(World world, State state, Config config, Tweener tweener)
		: SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private readonly Config _config = config;
		private readonly Tweener _tweener = tweener;
		private readonly QueryDescription _eventEntities =
			new QueryDescription().WithAll<EventNextWave>();

		public override void Update(in GameTime gameTime)
		{
			// Stop spawning new waves once the game is over.
			if (_state.GameOver)
			{
				return;
			}

			World.Query(
				in _eventEntities,
				(Entity entity, ref EventNextWave nextWaveEvent) =>
				{
					_state.WaveReady = false;
					_state.Wave++;

					_config.Waves.TryGetValue(_state.Wave, out var wave);

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
							_state.Wave < _state.MaxWaves
								? $"Wave {_state.Wave} of {_state.MaxWaves}"
								: "Final Wave!",
						Font = "pico-8",
					};

					World.Add(textEntity, text);
					World.Add(
						textEntity,
						new Blink(
							colors: [Pico8Color.Color5, Pico8Color.Color6, Pico8Color.Color7],
							colorSequence: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 0],
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

					if (_state.Wave > 1)
					{
						// TODO: play wave-complete sound
					}

					Tween lastTween = null;
					// The enemy whose fly-in tween finishes last; its OnEnd is reused
					// below to flip WaveReady, so we transition it to protect there too
					// (a tween only holds a single OnEnd delegate).
					EnemyState lastEnemyState = null;
					var lastTransitionsToProtect = false;

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
								_config.Entities.Enemies.GetEnemyConfig(enemyType)
								?? throw new System.Exception($"Enemy type {enemyType} not found!");

							var destinationX = ((x + 1) * 12) - 6;
							var destinationY = 4 + ((y + 1) * 12);

							var spawnPosition = new Vector2(
								x: (destinationX * 1.25f) - 16,
								y: destinationY - 66
							);
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
								_ => throw new System.Exception(
									$"Enemy type {enemyType} not found!"
								),
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
								new CollisionMask(
									CollisionMasks.Player | CollisionMasks.PlayerProjectile
								)
							);
							var enemyState = new EnemyState() { Value = EnemyStateType.Flyin };
							World.Add(enemyEntity, enemyState);
							World.Add(enemyEntity, new Health(enemy.StartingHealth));
							World.Add(
								enemyEntity,
								new Invulnerable() { Duration = tweenDuration + tweenDelay }
							);
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
							World.Add(enemyEntity, new EnemyType(enemyType));
							World.Add(enemyEntity, new TagEnemy());
							World.Add(enemyEntity, transform);

							// The boss keeps its own flow (M4); only formation enemies
							// transition into the protect state once they reach formation.
							var transitionsToProtect = enemyType != 5;

							lastTween = _tweener
								.TweenTo(
									target: transform,
									expression: transform => transform.Position,
									toValue: enemyDestination,
									duration: tweenDuration,
									delay: tweenDelay
								)
								.Easing(EasingFunctions.Linear);

							lastEnemyState = enemyState;
							lastTransitionsToProtect = transitionsToProtect;

							if (transitionsToProtect)
							{
								// In formation: eligible to be picked for attack/fire.
								lastTween.OnEnd(_ => enemyState.Value = EnemyStateType.Protect);
							}
						}
					}

					// Reusing lastTween's OnEnd (overwrites the per-enemy protect handler
					// above for that one enemy), so re-apply its protect transition here.
					lastTween.OnEnd(
						(action) =>
						{
							if (lastTransitionsToProtect && lastEnemyState != null)
							{
								lastEnemyState.Value = EnemyStateType.Protect;
							}

							_state.WaveReady = true;
						}
					);

					World.Destroy(entity);
				}
			);
		}
	}
}
