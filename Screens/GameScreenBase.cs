using System.Collections.Generic;
using Arch.Core;
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
			_tweener.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

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
