using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Spinning-ship behaviour: while attacking, once the ship has descended past the
	// player's Y it stops diving, turns toward the player along X and charges at vel.x
	// 60. One-shot — the TagLateralHunter marker is removed after the turn so it commits
	// to a single horizontal pass. Source: lateral-hunter-system.ts.
	public class LateralHunterSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _hunterQuery = new QueryDescription().WithAll<
			Direction,
			TagEnemy,
			TagLateralHunter,
			Transform,
			Velocity
		>();
		private readonly QueryDescription _playerQuery = new QueryDescription().WithAll<
			TagPlayer,
			Transform
		>();

		private readonly List<Entity> _hunters = [];

		public override void Update(in GameTime gameTime)
		{
			var player = GetPlayer();

			if (player == null)
			{
				return;
			}

			var playerPosition = World.Get<Transform>(player.Value).Position;

			_hunters.Clear();
			World.Query(in _hunterQuery, (Entity entity) => _hunters.Add(entity));

			foreach (var hunter in _hunters)
			{
				if (
					!World.TryGet<EnemyState>(hunter, out var state)
					|| state?.Value != EnemyStateType.Attack
				)
				{
					continue;
				}

				var position = World.Get<Transform>(hunter).Position;

				if (position.Y <= playerPosition.Y)
				{
					continue;
				}

				ref var direction = ref World.Get<Direction>(hunter);
				direction.Y = 0;
				direction.X = position.X > playerPosition.X ? -1 : 1;

				ref var velocity = ref World.Get<Velocity>(hunter);
				velocity.X = 60f;
				velocity.Y = 0f;

				// Committed to the charge: don't re-evaluate this enemy.
				World.Remove<TagLateralHunter>(hunter);
			}
		}

		private Entity? GetPlayer()
		{
			Entity? player = null;

			World.Query(in _playerQuery, (Entity entity) => player ??= entity);

			return player;
		}
	}
}
