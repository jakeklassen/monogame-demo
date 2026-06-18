using System;
using System.Collections.Generic;

namespace CherryBomb.Lib
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

	// A single scheduled callback. Durations are in milliseconds; the scheduler
	// converts the per-frame seconds delta internally. Set Repeat to re-arm the
	// timer after firing instead of dropping it. Source: timer.ts (TimeSpan).
	public sealed class ScheduledCallback(float durationMs, Action callback, bool repeat = false)
	{
		public float DurationMs { get; } = durationMs;
		public Action Callback { get; } = callback;
		public bool Repeat { get; } = repeat;
		public float ElapsedMs { get; private set; } = 0f;

		public bool IsExpired => ElapsedMs >= DurationMs;

		public void Update(float dtMs) => ElapsedMs += dtMs;

		public void Reset() => ElapsedMs = 0f;
	}

	// A per-screen timed-callback queue. Register delayed or repeating callbacks
	// in milliseconds; tick once per frame with the seconds delta from GameTime.
	// Non-repeating callbacks are removed after they fire; repeating ones re-arm.
	// Source: timer.ts (Timer) + systems/timer-system.ts.
	public sealed class Scheduler
	{
		private readonly List<ScheduledCallback> _callbacks = [];

		// Fire callback once after delayMs. Returns the handle so it can be removed.
		public ScheduledCallback Add(float delayMs, Action callback)
		{
			var entry = new ScheduledCallback(delayMs, callback);
			_callbacks.Add(entry);

			return entry;
		}

		// Fire callback every intervalMs until removed. Returns the handle.
		public ScheduledCallback AddRepeating(float intervalMs, Action callback)
		{
			var entry = new ScheduledCallback(intervalMs, callback, repeat: true);
			_callbacks.Add(entry);

			return entry;
		}

		public void Remove(ScheduledCallback callback) => _callbacks.Remove(callback);

		public void Clear() => _callbacks.Clear();

		// dtSeconds is the frame delta in seconds (as supplied by GameTime).
		public void Update(float dtSeconds)
		{
			var dtMs = dtSeconds * 1000f;

			// Iterate backwards so non-repeating callbacks can be removed safely.
			for (var i = _callbacks.Count - 1; i >= 0; i--)
			{
				var entry = _callbacks[i];

				entry.Update(dtMs);

				if (!entry.IsExpired)
				{
					continue;
				}

				entry.Callback();

				if (entry.Repeat)
				{
					entry.Reset();
				}
				else
				{
					_callbacks.RemoveAt(i);
				}
			}
		}
	}
}
