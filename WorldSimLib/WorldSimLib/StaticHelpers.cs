using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using WorldSimLib.Utils;

namespace WorldSimLib
{
	//It is common to create a class to contain all of your
	//extension methods. This class must be static.
	public static class ExtensionMethods
	{
		public static float Remap(this float value, float from1, float to1, float from2, float to2)
		{
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

		public static float Range(this System.Random rand, float minimum, float maximum)
		{
			return (float)rand.NextDouble() * (maximum - minimum) + minimum;
		}

		

		public static float positionInRange(this float value, float min, float max, bool clamp = true)
		{
			// min = 10
			// max = 1
			// value = 2
			value -= min; // -8
			max -= min; // -9
			min = 0;
			value = (value / (max - min)); // (-8 / (-9 -0) )
			if (clamp)
			{
				if (value < 0) { value = 0; }
				if (value > 1) { value = 1; }
			}
			return value;
		}

		public static List<Offer> Shuffle(this List<Offer> offers)
		{
			/*
			To shuffle an array a of n elements (indices 0..n-1):
			for i from n − 1 downto 1 do
				j ← random integer with 0 ≤ j ≤ i
				exchange a[j] and a[i]
			 */
			var arr = offers.ToArray();

			for (var i = 0; i < arr.Length; i++)
			{
				var ii = (arr.Length - 1) - i;
				if (ii > 1)
				{
					var j = StaticRandom.Instance.Next(0, ii + 1);
					var tmp = arr[j];
					arr[j] = arr[ii];
					arr[ii] = tmp;
				}
			}

			return new List<Offer>(arr);
		}

		public static float Average(float a, float b)
		{
			return (a + b) / 2;
		}

	}
}