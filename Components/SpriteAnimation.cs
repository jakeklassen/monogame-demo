using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Components
{
	public class AnimationDetails
	{
		public string Name { get; set; }
		public int SourceX { get; set; }
		public int SourceY { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int FrameWidth { get; set; }
		public int FrameHeight { get; set; }
	}

	public class SpriteAnimation
	{
		public float Delta { get; set; } = 0;
		public float Duration { get; set; } = 0;
		public float FrameRate { get; set; } = 0;
		public int CurrentFrame { get; set; } = 0;
		public bool Loop { get; set; } = true;
		public bool IsFinished { get; set; } = false;
		public Rectangle[] Frames { get; set; }
		public int[] FrameSequence { get; set; }
		public AnimationDetails AnimationDetails { get; set; }

		public static SpriteAnimation Factory(
			AnimationDetails animationDetails,
			float durationSeconds,
			bool loop = true,
			int[] frameSequence = null
		)
		{
			var delta = 0;
			var currentFrame = 0;
			var finished = false;
			var frames = new List<Rectangle>();

			var horizontalFrames = animationDetails.Width / animationDetails.FrameWidth;
			var verticalFrames = animationDetails.Height / animationDetails.FrameHeight;

			for (var i = 0; i < verticalFrames; i++)
			{
				var sourceY = animationDetails.SourceY + (i * animationDetails.FrameHeight);

				for (var j = 0; j < horizontalFrames; j++)
				{
					var sourceX = animationDetails.SourceX + (j * animationDetails.FrameWidth);

					frames.Add(
						new Rectangle(
							sourceX,
							sourceY,
							animationDetails.FrameWidth,
							animationDetails.FrameHeight
						)
					);
				}
			}

			// If no frame sequence is provided, use all frames.
			if (frameSequence == null)
			{
				frameSequence = new int[frames.Count];

				for (var i = 0; i < frames.Count; i++)
				{
					frameSequence[i] = i;
				}
			}

			// Determine the frame rate based on the duration of the animation
			// and the number of frames.
			// Also divide by 1000 to convert from milliseconds to seconds.
			var frameRate = durationSeconds / frameSequence.Length;

			return new SpriteAnimation()
			{
				AnimationDetails = animationDetails,
				Duration = durationSeconds,
				CurrentFrame = currentFrame,
				Delta = delta,
				FrameRate = frameRate,
				Frames = [.. frames],
				FrameSequence = frameSequence,
				IsFinished = finished,
				Loop = loop
			};
		}
	}
}
