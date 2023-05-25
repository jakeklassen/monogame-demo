using CherryBomb.Components;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace CherryBomb.Systems;

public class ShockwaveSystem : EntityUpdateSystem
{
	private ComponentMapper<Shockwave> _shockwaveMapper;

	public ShockwaveSystem() : base(Aspect.All(typeof(Shockwave)))
	{
	}

	public override void Initialize(IComponentMapperService mapperService)
	{
		_shockwaveMapper = mapperService.GetMapper<Shockwave>();
	}

	public override void Update(GameTime gameTime)
	{
		foreach (var entity in ActiveEntities)
		{
			var shockwave = _shockwaveMapper.Get(entity);

			shockwave.Radius += shockwave.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (shockwave.Radius >= shockwave.TargetRadius)
			{
				DestroyEntity(entity);
			}
		}
	}
}