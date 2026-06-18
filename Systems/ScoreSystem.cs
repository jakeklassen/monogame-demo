using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Diffs State.Score against the last seen value and, when it changes, rewrites the
	// "Score:N" content on the tagged HUD text entity. Update-only: the text itself is
	// drawn by TextRenderingSystem.
	public class ScoreSystem(World world, State state) : SystemBase<GameTime>(world)
	{
		private readonly State _state = state;
		private int _previousScore = -1;

		private readonly QueryDescription _scoreText = new QueryDescription().WithAll<
			TagScoreText,
			Text
		>();

		public override void Update(in GameTime gameTime)
		{
			if (_state.Score == _previousScore)
			{
				return;
			}

			_previousScore = _state.Score;

			World.Query(
				in _scoreText,
				(Entity entity, ref Text text) =>
				{
					text.Content = $"Score:{_state.Score}";
				}
			);
		}
	}
}
