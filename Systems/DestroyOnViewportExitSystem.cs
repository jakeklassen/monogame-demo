using CherryBomb;
using Components;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class DestroyOnViewportExitSystem : EntityUpdateSystem
	{
		private ComponentMapper<BoxCollider> _boxColliderMapper;
		private ComponentMapper<Transform> _transformMapper;

		public DestroyOnViewportExitSystem() : base(Aspect.All(typeof(BoxCollider), typeof(Transform)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_boxColliderMapper = mapperService.GetMapper<BoxCollider>();
			_transformMapper = mapperService.GetMapper<Transform>();
		}

		public override void Update(GameTime gameTime)
		{
			foreach (int entity in ActiveEntities)
			{
				BoxCollider boxCollider = _boxColliderMapper.Get(entity);
				Transform transform = _transformMapper.Get(entity);

				if (transform.Position.X < -boxCollider.Width ||
						transform.Position.X > Game1.TargetWidth + boxCollider.Width ||
						transform.Position.Y < -boxCollider.Height ||
						transform.Position.Y > Game1.TargetHeight + boxCollider.Height)
				{
					DestroyEntity(entity);
				}
			}
		}
	}
}