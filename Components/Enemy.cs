using Microsoft.Xna.Framework;

namespace SpaceDrift.Components
{
	public enum EnemyState
	{
		Patrol,
		Engage,
	}

	// A patrolling/pursuing foe. HitFlash/RespawnTimer drive feedback and
	// respawn; State/Waypoint/RepathTimer drive its AI.
	public struct Enemy
	{
		public int Health;
		public float HitFlash;
		public float RespawnTimer;
		public EnemyState State;
		public Vector2 Waypoint;
		public float RepathTimer;
	}
}
