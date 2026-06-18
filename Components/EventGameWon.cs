namespace CherryBomb.Components
{
	// Emitted once when the boss death sequence completes. No system consumes it
	// yet — the real GameWon screen transition lands in M5. For now its presence
	// simply marks that the game-won flow has fired. Mirrors EventGameOver.
	public class EventGameWon { }
}
