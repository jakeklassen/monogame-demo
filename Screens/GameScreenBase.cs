using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;

namespace CherryBomb.Screens
{
	// Thin scaffolding: each screen owns an Arch World and gets a typed Game
	// accessor. Subclasses drive their own update/draw (space-drift uses a
	// fixed-step accumulator, so there is no shared system-list iteration here).
	public abstract class GameScreenBase(Game1 game) : GameScreen(game)
	{
		protected new Game1 Game => (Game1)base.Game;
		protected readonly World _world = World.Create();

		public override void UnloadContent()
		{
			base.UnloadContent();

			World.Destroy(_world);
		}
	}
}
