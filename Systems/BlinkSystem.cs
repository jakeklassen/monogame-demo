using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	public class BlinkSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _blinkEntities = new QueryDescription().WithAll<Blink>();
		private GameTime _gameTime;

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _blinkEntities,
				(ref Blink blink) =>
				{
					blink.ElapsedSeconds += (float)_gameTime.ElapsedGameTime.TotalSeconds;

					if (blink.ElapsedSeconds >= blink.FrameRate)
					{
						blink.ElapsedSeconds = 0f;
						blink.CurrentColorIndex =
							(blink.CurrentColorIndex + 1) % blink.ColorSequence.Length;
					}

					blink.CurrentColor = blink.Colors[blink.ColorSequence[blink.CurrentColorIndex]];
				}
			);
		}
	}
}
