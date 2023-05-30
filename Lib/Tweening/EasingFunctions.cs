using System;
using Microsoft.Xna.Framework;

namespace Lib.Tweening
{
	public static class EasingFunctions
	{
		public static float Linear(float value)
		{
			return value;
		}

		public static float CubicIn(float value)
		{
			return Power.In(value, 3);
		}

		public static float CubicOut(float value)
		{
			return Power.Out(value, 3);
		}

		public static float CubicInOut(float value)
		{
			return Power.InOut(value, 3);
		}

		public static float QuadraticIn(float value)
		{
			return Power.In(value, 2);
		}

		public static float QuadraticOut(float value)
		{
			return Power.Out(value, 2);
		}

		public static float QuadraticInOut(float value)
		{
			return Power.InOut(value, 2);
		}

		public static float QuarticIn(float value)
		{
			return Power.In(value, 4);
		}

		public static float QuarticOut(float value)
		{
			return Power.Out(value, 4);
		}

		public static float QuarticInOut(float value)
		{
			return Power.InOut(value, 4);
		}

		public static float QuinticIn(float value)
		{
			return Power.In(value, 5);
		}

		public static float QuinticOut(float value)
		{
			return Power.Out(value, 5);
		}

		public static float QuinticInOut(float value)
		{
			return Power.InOut(value, 5);
		}

		public static float SineIn(float value)
		{
			return (float)Math.Sin((value * MathHelper.PiOver2) - MathHelper.PiOver2) + 1;
		}

		public static float SineOut(float value)
		{
			return (float)Math.Sin(value * MathHelper.PiOver2);
		}

		public static float SineInOut(float value)
		{
			return (float)(Math.Sin((value * MathHelper.Pi) - MathHelper.PiOver2) + 1) / 2;
		}

		public static float ExponentialIn(float value)
		{
			return (float)Math.Pow(2, 10 * (value - 1));
		}

		public static float ExponentialOut(float value)
		{
			return Out(value, ExponentialIn);
		}

		public static float ExponentialInOut(float value)
		{
			return InOut(value, ExponentialIn);
		}

		public static float CircleIn(float value)
		{
			return (float)-(Math.Sqrt(1 - (value * value)) - 1);
		}

		public static float CircleOut(float value)
		{
			return (float)Math.Sqrt(1 - ((value - 1) * (value - 1)));
		}

		public static float CircleInOut(float value)
		{
			return (float)(
				value <= .5
					? (Math.Sqrt(1 - (value * value * 4)) - 1) / -2
					: (Math.Sqrt(1 - (((value * 2) - 2) * ((value * 2) - 2))) + 1) / 2
			);
		}

		public static float ElasticIn(float value)
		{
			const int oscillations = 1;
			const float springiness = 3f;
			var e = (Math.Exp(springiness * value) - 1) / (Math.Exp(springiness) - 1);
			return (float)(
				e * Math.Sin((MathHelper.PiOver2 + (MathHelper.TwoPi * oscillations)) * value)
			);
		}

		public static float ElasticOut(float value)
		{
			return Out(value, ElasticIn);
		}

		public static float ElasticInOut(float value)
		{
			return InOut(value, ElasticIn);
		}

		public static float BackIn(float value)
		{
			const float amplitude = 1f;
			return (float)(Math.Pow(value, 3) - (value * amplitude * Math.Sin(value * MathHelper.Pi)));
		}

		public static float BackOut(float value)
		{
			return Out(value, BackIn);
		}

		public static float BackInOut(float value)
		{
			return InOut(value, BackIn);
		}

		public static float BounceOut(float value)
		{
			return Out(value, BounceIn);
		}

		public static float BounceInOut(float value)
		{
			return InOut(value, BounceIn);
		}

		public static float BounceIn(float value)
		{
			const float bounceConst1 = 2.75f;
			var bounceConst2 = (float)Math.Pow(bounceConst1, 2);

			value = 1 - value; //flip x-axis

			if (value < 1 / bounceConst1) // big bounce
			{
				return 1f - (bounceConst2 * value * value);
			}

			if (value < 2 / bounceConst1)
			{
				return 1 - (float)((bounceConst2 * Math.Pow(value - (1.5f / bounceConst1), 2)) + .75);
			}

			if (value < 2.5 / bounceConst1)
			{
				return 1 - (float)((bounceConst2 * Math.Pow(value - (2.25f / bounceConst1), 2)) + .9375);
			}

			//small bounce
			return 1f - (float)((bounceConst2 * Math.Pow(value - (2.625f / bounceConst1), 2)) + .984375);
		}

		private static float Out(float value, Func<float, float> function)
		{
			return 1 - function(1 - value);
		}

		private static float InOut(float value, Func<float, float> function)
		{
			return value < 0.5f ? 0.5f * function(value * 2) : 1f - (0.5f * function(2 - (value * 2)));
		}

		private static class Power
		{
			public static float In(double value, int power)
			{
				return (float)Math.Pow(value, power);
			}

			public static float Out(double value, int power)
			{
				var sign = power % 2 == 0 ? -1 : 1;
				return (float)(sign * (Math.Pow(value - 1, power) + sign));
			}

			public static float InOut(double s, int power)
			{
				s *= 2;

				if (s < 1)
				{
					return In(s, power) / 2;
				}

				var sign = power % 2 == 0 ? -1 : 1;
				return (float)(sign / 2.0 * (Math.Pow(s - 2, power) + (sign * 2)));
			}
		}
	}
}
