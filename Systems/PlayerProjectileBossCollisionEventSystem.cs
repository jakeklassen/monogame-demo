using Arch.Core;
using CherryBomb;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Boss-specific damage path. Player projectiles that hit the boss emit
	// EventPlayerProjectileBossCollision (from CollisionSystem) instead of the
	// generic enemy-collision event, so the boss is never destroyed by the
	// enemy-death path. On each hit the boss swaps to its hurt frame for 0.2s
	// (a hurt flash) and loses the projectile's damage; at health <= 0 it emits
	// EventDestroyBoss and stops hurting.
	//
	// Deviation from the TS source: the original swaps two PICO-8 palette colours
	// (Color3->Color8, Color11->Color14) per-pixel while hurting. MonoGame has no
	// per-sprite palette remap without a shader, so we approximate the hurt tell
	// with the dedicated boss hurt frame plus a brief red Flash (reusing the
	// existing Flash/FlashRenderingSystem path) rather than adding a shader.
	// Source: player-projectile-boss-collision-event-system.ts.
	public class PlayerProjectileBossCollisionEventSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _eventQuery =
			new QueryDescription().WithAll<EventPlayerProjectileBossCollision>();

		private readonly QueryDescription _bossQuery = new QueryDescription().WithAll<
			TagBoss,
			Sprite,
			SpriteAnimation
		>();

		// Locks the boss into the hurt frame for a beat so the animation doesn't
		// snap back to idle too quickly.
		private const float HurtDurationSeconds = 0.2f;

		// Once the boss is dead we stop reverting to idle so it stays in the hurt
		// frame through its death sequence.
		private bool _disableHurt;
		private float _hurtTimer = HurtDurationSeconds;

		public override void Update(in GameTime gameTime)
		{
			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (!_disableHurt)
			{
				_hurtTimer += dt;
			}

			// Revert to idle + drop the palette swap once the hurt window elapses.
			// Materialize the boss first so the structural Remove<PaletteSwap> isn't
			// done mid-query.
			if (_hurtTimer >= HurtDurationSeconds && !_disableHurt)
			{
				var bosses = new Entity[World.CountEntities(in _bossQuery)];
				World.GetEntities(in _bossQuery, bosses, 0);

				foreach (var boss in bosses)
				{
					var animation = World.Get<SpriteAnimation>(boss);

					if (animation.AnimationDetails.Name != "boss-idle")
					{
						SetBossAnimation(boss, "boss-idle", "Idle");
					}

					if (World.Has<PaletteSwap>(boss))
					{
						World.Remove<PaletteSwap>(boss);
					}
				}
			}

			World.Query(
				in _eventQuery,
				(Entity entity, ref EventPlayerProjectileBossCollision collisionEvent) =>
				{
					var projectile = collisionEvent.ProjectileEntity;
					var boss = collisionEvent.BossEntity;

					// Bullet shockwave (only for the small bullet, like the TS).
					if (World.Has<TagBullet>(projectile) && World.Has<Transform>(projectile))
					{
						var projectileTransform = World.Get<Transform>(projectile);
						var projectileSprite = World.Has<Sprite>(projectile)
							? World.Get<Sprite>(projectile)
							: default;

						World.Create(
							new Shockwave(
								radius: 3,
								targetRadius: 6,
								color: Pico8Color.Color9,
								speed: 30
							),
							new Transform(
								position: new Vector2(
									projectileTransform.Position.X
										+ (projectileSprite.CurrentFrame.Width / 2f),
									projectileTransform.Position.Y
										+ (projectileSprite.CurrentFrame.Height / 2f)
								),
								rotation: 0f,
								scale: Vector2.One
							)
						);
					}

					if (World.IsAlive(projectile))
					{
						World.Destroy(projectile);
					}

					// Invulnerable (still flying in) or already dead: ignore damage.
					if (
						!World.IsAlive(boss)
						|| World.Has<Invulnerable>(boss)
						|| !World.Has<Health>(boss)
					)
					{
						World.Destroy(entity);

						return;
					}

					_hurtTimer = 0f;

					// Swap to the hurt frame + the palette swap (green -> red/pink),
					// matching the TS source's sprite.paletteSwaps. PaletteSwap routes
					// the boss through PaletteSwapRenderingSystem for the hurt window.
					if (World.Has<SpriteAnimation>(boss))
					{
						SetBossAnimation(boss, "boss-hurt", "Hurt");
					}

					if (World.Has<Sprite>(boss))
					{
						ref var sprite = ref World.Get<Sprite>(boss);
						sprite.CurrentFrame = SpriteSheet.Enemies.Boss.HurtFrame;
					}

					if (!World.Has<PaletteSwap>(boss))
					{
						World.Add(boss, new PaletteSwap());
					}

					ref var health = ref World.Get<Health>(boss);
					health.Amount -= collisionEvent.Damage;

					if (health.Amount <= 0)
					{
						_disableHurt = true;

						World.Create(new EventDestroyBoss());
					}

					World.Destroy(entity);
				}
			);
		}

		// Replaces the boss SpriteAnimation with the named animation from the
		// spritesheet (durations match the TS: hurt 100ms, idle 400ms).
		private void SetBossAnimation(Entity boss, string name, string spriteSheetKey)
		{
			var data = SpriteSheet.Enemies.Boss.Animations[spriteSheetKey];
			var durationSeconds = spriteSheetKey == "Idle" ? 0.4f : 0.1f;

			World.Set(
				boss,
				SpriteAnimation.Factory(
					animationDetails: new AnimationDetails()
					{
						Name = name,
						SourceX = data.SourceX,
						SourceY = data.SourceY,
						Width = data.Width,
						Height = data.Height,
						FrameWidth = data.FrameWidth,
						FrameHeight = data.FrameHeight,
					},
					durationSeconds: durationSeconds,
					loop: true
				)
			);
		}
	}
}
