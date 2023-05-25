namespace CherryBomb;

public class Timer
{
	public float Duration { get; private set; } = 0;
	public float Elapsed { get; private set; } = 0;
	public bool IsExpired { get; private set; } = false;

	public Timer(float duration)
	{
		Duration = duration;
	}

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