using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;
using SoundEffect = Microsoft.Xna.Framework.Audio.SoundEffect;
using SoundEffectInstance = Microsoft.Xna.Framework.Audio.SoundEffectInstance;

namespace CherryBomb.Systems
{
	// Consumes EventPlaySound event entities and plays the matching cached
	// SoundEffect, then destroys the entity. Looping sounds (music) are played
	// through retained SoundEffectInstances so they can be stopped on screen exit.
	//
	// All 18 tracks are SoundEffects (no Song/MediaPlayer) so the MGCB content
	// pipeline builds cleanly cross-platform without ffmpeg.
	public class SoundSystem(World world, Dictionary<string, SoundEffect> soundCache)
		: SystemBase<GameTime>(world)
	{
		private readonly Dictionary<string, SoundEffect> _soundCache = soundCache;

		// Retained looping instances, keyed by track, so StopAll can halt them.
		private readonly Dictionary<string, SoundEffectInstance> _loopingInstances = new();

		private readonly QueryDescription _query = new QueryDescription().WithAll<EventPlaySound>();

		/// <summary>
		///     Emits a sound event entity that <see cref="SoundSystem"/> will consume.
		/// </summary>
		public static void Play(World world, string track, bool loop = false)
		{
			var entity = world.Create();
			world.Add(entity, new EventPlaySound(track, loop));
		}

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _query,
				(Entity entity, ref EventPlaySound playSound) =>
				{
					if (_soundCache.TryGetValue(playSound.Track, out var soundEffect))
					{
						if (playSound.Loop)
						{
							// Reuse an existing instance for this track if present,
							// otherwise create and retain one for later stopping.
							if (!_loopingInstances.TryGetValue(playSound.Track, out var instance))
							{
								instance = soundEffect.CreateInstance();
								_loopingInstances.Add(playSound.Track, instance);
							}

							instance.IsLooped = true;
							instance.Play();
						}
						else
						{
							// Fire-and-forget one-shot.
							soundEffect.Play();
						}
					}

					World.Destroy(entity);
				}
			);
		}

		/// <summary>
		///     Stops and disposes all retained looping instances. Call on screen exit.
		/// </summary>
		public void StopAll()
		{
			foreach (var instance in _loopingInstances.Values)
			{
				instance.Stop();
				instance.Dispose();
			}

			_loopingInstances.Clear();
		}
	}
}
