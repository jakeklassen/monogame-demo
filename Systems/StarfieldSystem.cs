using System;

using Components;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class StarfieldSystem : EntityUpdateSystem
	{
		private ComponentMapper<Transform> _transformMapper;
		private readonly Random _random = new();

		public StarfieldSystem() : base(Aspect.All(typeof(Star), typeof(Transform)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Update(GameTime gameTime)
		{
			foreach (var entity in ActiveEntities)
			{
				var transform = _transformMapper.Get(entity);

				if (transform.Position.Y > 128)
				{
					transform.Position = new Vector2(_random.Next(1, 127), -1);
				}
			}
		}
	}
}