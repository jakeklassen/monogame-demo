using Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class SpriteAnimationSystem : EntityProcessingSystem
	{
		public SpriteAnimationSystem() : base(Aspect.All(typeof(Sprite), typeof(SpriteAnimation)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
		}

		public override void Process(GameTime gameTime, int entityId)
		{
			var entity = GetEntity(entityId);
			var sprite = entity.Get<Sprite>();
			var spriteAnimation = entity.Get<SpriteAnimation>();

			if (spriteAnimation.IsFinished && !spriteAnimation.Loop)
			{
				entity.Detach<SpriteAnimation>();

				return;
			}

			spriteAnimation.Delta += (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (spriteAnimation.Delta >= spriteAnimation.FrameRate)
			{
				spriteAnimation.Delta = 0;

				spriteAnimation.CurrentFrame =
									(spriteAnimation.CurrentFrame + 1) %
									spriteAnimation.FrameSequence.Length;

				var frameIndex = spriteAnimation.FrameSequence[spriteAnimation.CurrentFrame];
				sprite.CurrentFrame = spriteAnimation.Frames[frameIndex];

				if (spriteAnimation.CurrentFrame == spriteAnimation.FrameSequence.Length - 1)
				{
					spriteAnimation.IsFinished = true;
				}
			}
		}
	}
}