namespace CherryBomb.Components
{
	// Short-lived event entity carrying a sound request. Emitted by gameplay
	// systems (via SoundSystem.Play) and consumed + destroyed by SoundSystem.
	public class EventPlaySound(string track, bool loop = false)
	{
		public string Track { get; set; } = track;
		public bool Loop { get; set; } = loop;
	}
}
