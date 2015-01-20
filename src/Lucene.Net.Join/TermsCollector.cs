/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Join;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Join
{
	/// <summary>A collector that collects all terms from a specified field matching the query.
	/// 	</summary>
	/// <remarks>A collector that collects all terms from a specified field matching the query.
	/// 	</remarks>
	/// <lucene.experimental></lucene.experimental>
	internal abstract class TermsCollector : Collector
	{
		internal readonly string field;

		internal readonly BytesRefHash collectorTerms = new BytesRefHash();

		internal TermsCollector(string field)
		{
			this.field = field;
		}

		public virtual BytesRefHash GetCollectorTerms()
		{
			return collectorTerms;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return true;
		}

		/// <summary>
		/// Chooses the right
		/// <see cref="TermsCollector">TermsCollector</see>
		/// implementation.
		/// </summary>
		/// <param name="field">The field to collect terms for</param>
		/// <param name="multipleValuesPerDocument">Whether the field to collect terms for has multiple values per document.
		/// 	</param>
		/// <returns>
		/// a
		/// <see cref="TermsCollector">TermsCollector</see>
		/// instance
		/// </returns>
		internal static Lucene.Net.Search.Join.TermsCollector Create(string field, 
			bool multipleValuesPerDocument)
		{
			return multipleValuesPerDocument ? new TermsCollector.MV(field) : new TermsCollector.SV
				(field);
		}

		internal class MV : TermsCollector
		{
			internal readonly BytesRef scratch = new BytesRef();

			private SortedSetDocValues docTermOrds;

			internal MV(string field) : base(field)
			{
			}

			// impl that works with multiple values per document
			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				docTermOrds.SetDocument(doc);
				long ord;
				while ((ord = docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
				{
					docTermOrds.LookupOrd(ord, scratch);
					collectorTerms.Add(scratch);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(((AtomicReader)context.Reader()), 
					field);
			}
		}

		internal class SV : TermsCollector
		{
			internal readonly BytesRef spare = new BytesRef();

			private BinaryDocValues fromDocTerms;

			internal SV(string field) : base(field)
			{
			}

			// impl that works with single value per document
			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				fromDocTerms.Get(doc, spare);
				collectorTerms.Add(spare);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				fromDocTerms = FieldCache.DEFAULT.GetTerms(((AtomicReader)context.Reader()), field
					, false);
			}
		}
	}
}
