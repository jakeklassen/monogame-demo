namespace Lib
{
	public class Timer(float duration)
	{
		public float Duration { get; private set; } = duration;
		public float Elapsed { get; private set; } = 0;
		public bool IsExpired { get; private set; } = false;

		public void Update(float dt)
		{
			Elapsed += dt;

			if (Elapsed >= Duration)
			{
				IsExpired = true;
			}
		}

		public void Reset()
		{
			Elapsed = 0;
			IsExpired = false;
		}
	}
}
