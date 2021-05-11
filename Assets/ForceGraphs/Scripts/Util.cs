using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Assets.ForceGraphs.Scripts
{
	public class Util
	{
		private static Util instance;
		private Random random;
		private static readonly int[] digits = {0,1,2,3,4,5,6,7,8,9};

		private static Util Instance
		{
			get { return instance ?? (instance = new Util()); }
		}

		public Util()
		{
			random = new Random();
		}

		public static int Lerp(int min, int max, float amount)
		{
			return (int)Math.Ceiling((max - min) * amount) + min;
		}

		public static float Lerp(float min, float max, float amount)
		{
			return ((max - min) * amount) + min;
		}

		public static float Random()
		{
			return (float)Instance.random.NextDouble();
		}

		public static byte RandomByte()
		{
			return Convert.ToByte(Instance.random.Next() % 255);
		}

		public static int RandomRange(int min, int max)
		{
			return Instance.random.Next(min, max);
		}

		// CREDIT: http://stackoverflow.com/questions/6651554/random-number-in-long-range-is-this-the-way
		public static long RandomRange(long min, long max)
		{
			var buf = new byte[8];
			Instance.random.NextBytes(buf);
			var longRand = BitConverter.ToInt64(buf, 0);

			return (Math.Abs(longRand % (max - min)) + min);
		}

		public static float RandomRange(float min, float max)
		{
			return ((max - min) * (float)Instance.random.NextDouble()) + min;
		}

		public static T RandomElement<T>(T[] array)
		{
			if (array == null)
				return default(T);

			if (!array.Any())
				return default(T);

			return array[Instance.random.Next(0, array.Length)];
		}

		public static T RandomElement<T>(IEnumerable<T> list)
		{
			return RandomElement(list.ToArray());
		}

		public static T RandomElement<T>(Array array)
		{
			if (array == null)
				return default(T);

			if (array.Length == 0)
				return default(T);

			return (T)array.GetValue(Instance.random.Next(0, array.Length));
		}

		public static bool FlipCoin()
		{
			return Instance.random.Next(0, 2) == 1;
		}

		public static bool Chance(float chance)
		{
			return Random() <= chance;
		}

		public static void SetSeed(int seed)
		{
			Instance.random = new Random(seed);
		}

		public static void SetRandomSeed()
		{
			Instance.random = new Random();
		}

		public static string GetTimeDeltaDescription(int delta)
		{
			return String.Format("{0} day{1} ago", delta, delta > 1 ? "s" : "");
		}

		public static void RunTests()
		{
//			SetRandomSeed();
//			Templates.GetRandomOrganizationName("CriminalRecordsCentre");
//			Templates.GetRandomOrganizationName("SoftwareCompany");
		}

		public static void PopulateFromJSONObject(object obj, string path)
		{
			try
			{
				JsonSerializer.CreateDefault(new JsonSerializerSettings()).Populate(JObject.Parse(File.ReadAllText(path)).CreateReader(), obj);
			}
			catch(JsonReaderException e)
			{
				throw new Exception("Error while parsing JSON file " + path + ": " + e.Message);
			}
		}

		public static void PopulateFromJSONArray(object obj, string path)
		{
			try
			{
				JsonSerializer.CreateDefault(new JsonSerializerSettings()).Populate(JArray.Parse(File.ReadAllText(path)).CreateReader(), obj);
			}
			catch (JsonReaderException e)
			{
				throw new Exception("Error while parsing JSON file " + path + ": " + e.Message);
			}
		}

		public static string Capitalize(string str)
		{
			return Char.ToUpper(str[0]) + str.Substring(1);
		}

		public static string CapitalizeWords(string sentance)
		{
			var spaceWords = sentance.Split(new[] { ' ' });
			var result = string.Join(" ", spaceWords.Select(w => (w != "of" && w != "and") ? Capitalize(w) : w).ToArray());
			var dashWords = result.Split(new[] { '-' });
			result = string.Join("-", dashWords.Select(w => (w != "of" && w != "and") ? Capitalize(w) : w).ToArray());

			return result;
		}

		public static int RandomDigit()
		{
			return RandomElement(digits);
		}

		// Credit: http://stackoverflow.com/questions/10390356/c-sharp-serializing-deserializing-with-memory-stream
		public static MemoryStream SerializeToStream(object o)
		{
			var stream = new MemoryStream();
			var dcs = new DataContractSerializer(o.GetType());
//			var xdw = XmlDictionaryWriter.CreateTextWriter(stream);
			dcs.WriteObject(stream, o);
//			xdw.Flush();

			return stream;
		}
//		public static object DeserializeFromStream(MemoryStream stream)
//		{
//			IFormatter formatter = new BinaryFormatter();
//			stream.Seek(0, SeekOrigin.Begin);
//			object o = formatter.Deserialize(stream);
//			return o;
//		}
		public static string FormatLargeNumber(long number)
		{
			string result = "";
			if (number < 1000000)
				result = string.Format("{0:N}", number);
			else if(number < 1000000000)
				result = string.Format("{0:N}M", number / 1000000f);
			else if (number < 1000000000000)
				result = string.Format("{0:N}B", number / 1000000000f);
			else if (number < 1000000000000000)
				result = string.Format("{0:N}T", number / 1000000000000f);

			result = result.Replace(".00", "");

			return result;
		}
	}

	// Credit: http://stackoverflow.com/questions/2683442/where-can-i-find-the-clamp-function-in-net
	public static class MathExtensionMethods
	{
		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0) return min;
			return val.CompareTo(max) > 0 ? max : val;
		}
	}

	public static class StringExtensionMethods
	{
		// Credit: http://stackoverflow.com/questions/8809354/replace-first-occurrence-of-pattern-in-a-string
		public static string ReplaceFirst(this string text, string search, string replace)
		{
			var pos = text.IndexOf(search);
			if (pos < 0)
			{
				return text;
			}
			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
		}

		// Credit: http://stackoverflow.com/questions/9367119/replacing-a-char-at-a-given-index-in-string
		public static string ReplaceAt(this string input, int index, char newChar)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			var chars = input.ToCharArray();
			chars[index] = newChar;
			return new string(chars);
		}
	}

	class WritablePropertiesOnlyResolver : DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var props = base.CreateProperties(type, memberSerialization);
			return props.Where(p => p.Writable).ToList();
		}
	}
}
