using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class SpriteAnimationSystem(World world) : SystemBase<GameTime>(world)
	{
		private GameTime _gameTime;
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			Sprite,
			SpriteAnimation
		>();

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _query,
				(Entity entity, ref Sprite sprite, ref SpriteAnimation spriteAnimation) =>
				{
					if (spriteAnimation.IsFinished && !spriteAnimation.Loop)
					{
						World.Remove<SpriteAnimation>(entity);

						return;
					}

					spriteAnimation.Delta += (float)_gameTime.ElapsedGameTime.TotalSeconds;

					if (spriteAnimation.Delta >= spriteAnimation.FrameRate)
					{
						spriteAnimation.Delta = 0;

						spriteAnimation.CurrentFrame =
							(spriteAnimation.CurrentFrame + 1) % spriteAnimation.FrameSequence.Length;

						var frameIndex = spriteAnimation.FrameSequence[spriteAnimation.CurrentFrame];
						sprite.CurrentFrame = spriteAnimation.Frames[frameIndex];

						if (spriteAnimation.CurrentFrame == spriteAnimation.FrameSequence.Length - 1)
						{
							spriteAnimation.IsFinished = true;
						}
					}
				}
			);
		}
	}
}
