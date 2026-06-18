using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Diffs State.Cherries and, when it changes, rewrites the cherry-count HUD text.
	// Update-only: TextRenderingSystem draws the text.
	public class CherryTextSystem(World world, State state) : SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private int _previousCherries = -1;

		private readonly QueryDescription _cherryText = new QueryDescription().WithAll<
			TagCherryText,
			Text
		>();

		public override void Update(in GameTime gameTime)
		{
			if (_state.Cherries == _previousCherries)
			{
				return;
			}

			_previousCherries = _state.Cherries;

			World.Query(
				in _cherryText,
				(Entity entity, ref Text text) =>
				{
					text.Content = $"{_state.Cherries}";
				}
			);
		}
	}
}
