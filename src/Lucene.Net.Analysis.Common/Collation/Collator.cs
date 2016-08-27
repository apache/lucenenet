using System;
using System.Globalization;

namespace Lucene.Net.Analysis.Collation
{
	//TODO: This should be made abstract and a default implementation should be created.
	public class Collator
	{
		private readonly CultureInfo culture;

		public static Int32 Primary = 0;
		public static Int32 Secondary = 1;
		public static Int32 Tertiary = 2;
		public static Int32 Identical = 3;
		public static Int32 NoDecomposition = 0;
		public static Int32 CannonicalDecomposition = 1;
		public static Int32 FullDecomposition = 2;
		private readonly CompareOptions options;

		public Collator(CultureInfo culture, CompareOptions options)
		{
			this.culture = culture;
			this.options = options;
		}

		public Int32 Strength { get; set; }
		public Int32 Decomposition { get; set; }

		public SortKey GetCollationKey(String source)
		{
			return this.culture.CompareInfo.GetSortKey(source);
		}

		public static Collator GetInstance(CultureInfo culture)
		{
			return new Collator(culture, CompareOptions.None);
		}

		public static Collator GetInstance(CultureInfo culture, CompareOptions options)
		{
			return new Collator(culture, options);
		}

		public Int32 Compare(String s, String s1)
		{
			return String.Compare(s, s1, this.culture, this.options);
		}
	}
}
