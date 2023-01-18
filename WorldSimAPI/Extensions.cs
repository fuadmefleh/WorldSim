using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace WorldSimAPI
{
    public static class Extensions
    {
		static Random rng = new Random();

		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static Direction GetOpposite(this Direction value )
		{
			switch( value )
			{
				case Direction.East:
					return Direction.West;
				case Direction.West:
					return Direction.East;
				case Direction.SouthWest:
					return Direction.NorthEast;
				case Direction.NorthEast:
					return Direction.SouthWest;
				case Direction.SouthEast:
					return Direction.NorthWest;
				case Direction.NorthWest:
					return Direction.SouthEast;
			}

			return Direction.North;
		}

		public static string GetDescription(this Enum value)
		{
			Type type = value.GetType();
			string name = Enum.GetName(type, value);
			if (name != null)
			{
				FieldInfo field = type.GetField(name);
				if (field != null)
				{
					DescriptionAttribute attr =
						   Attribute.GetCustomAttribute(field,
							 typeof(DescriptionAttribute)) as DescriptionAttribute;
					if (attr != null)
					{
						return attr.Description;
					}
				}
			}
			return null;
		}
	}
}
