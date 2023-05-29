using Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class BlinkSystem : EntityProcessingSystem
	{
		private ComponentMapper<Blink> _blinkMapper;

		public BlinkSystem() : base(Aspect.All(typeof(Blink)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_blinkMapper = mapperService.GetMapper<Blink>();
		}

		public override void Process(GameTime gameTime, int entityId)
		{
			var blink = _blinkMapper.Get(entityId);

			blink.ElapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (blink.ElapsedSeconds >= blink.FrameRate)
			{
				blink.ElapsedSeconds = 0f;
				blink.CurrentColorIndex = (blink.CurrentColorIndex + 1) % blink.ColorSequence.Length;
			}

			blink.CurrentColor = blink.Colors[blink.ColorSequence[blink.CurrentColorIndex]];
		}
	}
}