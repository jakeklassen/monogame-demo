using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Yellow-ship attack behaviour: while attacking it hovers and roughly once a second
	// fires an 8-bullet spread; once it sinks past y 110 it commits to leaving by
	// descending at vel.y 30 (DestroyOnViewportExitSystem cleans it up off-screen).
	// Source: yellow-ship-system.ts.
	public class YellowShipSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			EnemyState,
			TagYellowShip,
			Transform,
			Velocity
		>();

		private readonly List<Entity> _ships = [];

		private float _spreadshotTimer;

		public override void Update(in GameTime gameTime)
		{
			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_spreadshotTimer += dt;

			var fireReady = _spreadshotTimer > 1f;

			_ships.Clear();
			World.Query(in _query, (Entity entity) => _ships.Add(entity));

			foreach (var ship in _ships)
			{
				var state = World.Get<EnemyState>(ship).Value;

				if (state != EnemyStateType.Attack)
				{
					continue;
				}

				var position = World.Get<Transform>(ship).Position;

				if (position.Y > 110f)
				{
					ref var velocity = ref World.Get<Velocity>(ship);
					velocity.Y = 30f;
				}
				else if (fireReady)
				{
					EnemyBulletFactory.FireSpread(World, ship, count: 8, speed: 40f);
				}
			}

			if (fireReady)
			{
				_spreadshotTimer = 0f;
			}
		}
	}
}
