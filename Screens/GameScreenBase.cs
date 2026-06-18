using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Lib;
using CherryBomb.Lib.Tweening;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;

namespace CherryBomb.Screens
{
	// Shared scaffolding for screens: each owns an Arch World, a Tweener, and
	// ordered update/draw system lists. Subclasses spawn entities and register
	// systems in LoadContent; this base drives the per-frame iteration and teardown.
	public abstract class GameScreenBase(Game1 game) : GameScreen(game)
	{
		protected new Game1 Game => (Game1)base.Game;
		protected readonly Tweener _tweener = new();

		// Per-screen scheduler for delayed/repeating callbacks (ms). Ticked below
		// with the frame delta in seconds; used by gameplay for staggered spawns,
		// timed delays, etc.
		protected readonly Scheduler _scheduler = new();
		protected readonly World _world = World.Create();
		protected readonly List<SystemBase<GameTime>> _updateSystems = [];
		protected readonly List<SystemBase<GameTime>> _drawSystems = [];

		public override void UnloadContent()
		{
			base.UnloadContent();

			World.Destroy(_world);
		}

		public override void Update(GameTime gameTime)
		{
			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			_tweener.Update(dt);
			_scheduler.Update(dt);

			foreach (var system in _updateSystems)
			{
				system.Update(gameTime);
			}
		}

		public override void Draw(GameTime gameTime)
		{
			foreach (var system in _drawSystems)
			{
				system.Update(gameTime);
			}
		}
	}
}
