using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Advances each SpriteOutlineAnimation's color sequence and writes the current
	// color back onto the paired SpriteOutline so the renderer picks it up. Mirrors
	// BlinkSystem. Source: sprite-outline-animation-system.ts.
	public class SpriteOutlineAnimationSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			SpriteOutline,
			SpriteOutlineAnimation
		>();

		public override void Update(in GameTime gameTime)
		{
			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			World.Query(
				in _query,
				(Entity entity) =>
				{
					var outline = World.Get<SpriteOutline>(entity);
					var animation = World.Get<SpriteOutlineAnimation>(entity);

					animation.ElapsedSeconds += dt;

					if (animation.ElapsedSeconds >= animation.FrameRate)
					{
						animation.ElapsedSeconds = 0f;
						animation.CurrentColorIndex =
							(animation.CurrentColorIndex + 1) % animation.ColorSequence.Length;
					}

					animation.CurrentColor = animation.Colors[
						animation.ColorSequence[animation.CurrentColorIndex]
					];

					outline.Color = animation.CurrentColor;
				}
			);
		}
	}
}
