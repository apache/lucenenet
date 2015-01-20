/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Join;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Join
{
	internal abstract class TermsWithScoreCollector : Collector
	{
		private const int INITIAL_ARRAY_SIZE = 256;

		internal readonly string field;

		internal readonly BytesRefHash collectedTerms = new BytesRefHash();

		internal readonly ScoreMode scoreMode;

		internal Scorer scorer;

		internal float[] scoreSums = new float[INITIAL_ARRAY_SIZE];

		internal TermsWithScoreCollector(string field, ScoreMode scoreMode)
		{
			this.field = field;
			this.scoreMode = scoreMode;
		}

		public virtual BytesRefHash GetCollectedTerms()
		{
			return collectedTerms;
		}

		public virtual float[] GetScoresPerTerm()
		{
			return scoreSums;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			this.scorer = scorer;
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return true;
		}

		/// <summary>
		/// Chooses the right
		/// <see cref="TermsWithScoreCollector">TermsWithScoreCollector</see>
		/// implementation.
		/// </summary>
		/// <param name="field">The field to collect terms for</param>
		/// <param name="multipleValuesPerDocument">Whether the field to collect terms for has multiple values per document.
		/// 	</param>
		/// <returns>
		/// a
		/// <see cref="TermsWithScoreCollector">TermsWithScoreCollector</see>
		/// instance
		/// </returns>
		internal static Org.Apache.Lucene.Search.Join.TermsWithScoreCollector Create(string
			 field, bool multipleValuesPerDocument, ScoreMode scoreMode)
		{
			if (multipleValuesPerDocument)
			{
				switch (scoreMode)
				{
					case ScoreMode.Avg:
					{
						return new TermsWithScoreCollector.MV.Avg(field);
					}

					default:
					{
						return new TermsWithScoreCollector.MV(field, scoreMode);
						break;
					}
				}
			}
			else
			{
				switch (scoreMode)
				{
					case ScoreMode.Avg:
					{
						return new TermsWithScoreCollector.SV.Avg(field);
					}

					default:
					{
						return new TermsWithScoreCollector.SV(field, scoreMode);
						break;
					}
				}
			}
		}

		internal class SV : TermsWithScoreCollector
		{
			internal readonly BytesRef spare = new BytesRef();

			internal BinaryDocValues fromDocTerms;

			internal SV(string field, ScoreMode scoreMode) : base(field, scoreMode)
			{
			}

			// impl that works with single value per document
			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				fromDocTerms.Get(doc, spare);
				int ord = collectedTerms.Add(spare);
				if (ord < 0)
				{
					ord = -ord - 1;
				}
				else
				{
					if (ord >= scoreSums.Length)
					{
						scoreSums = ArrayUtil.Grow(scoreSums);
					}
				}
				float current = scorer.Score();
				float existing = scoreSums[ord];
				if (float.Compare(existing, 0.0f) == 0)
				{
					scoreSums[ord] = current;
				}
				else
				{
					switch (scoreMode)
					{
						case ScoreMode.Total:
						{
							scoreSums[ord] = scoreSums[ord] + current;
							break;
						}

						case ScoreMode.Max:
						{
							if (current > existing)
							{
								scoreSums[ord] = current;
							}
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				fromDocTerms = FieldCache.DEFAULT.GetTerms(((AtomicReader)context.Reader()), field
					, false);
			}

			internal class Avg : TermsWithScoreCollector.SV
			{
				internal int[] scoreCounts = new int[INITIAL_ARRAY_SIZE];

				internal Avg(string field) : base(field, ScoreMode.Avg)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void Collect(int doc)
				{
					fromDocTerms.Get(doc, spare);
					int ord = collectedTerms.Add(spare);
					if (ord < 0)
					{
						ord = -ord - 1;
					}
					else
					{
						if (ord >= scoreSums.Length)
						{
							scoreSums = ArrayUtil.Grow(scoreSums);
							scoreCounts = ArrayUtil.Grow(scoreCounts);
						}
					}
					float current = scorer.Score();
					float existing = scoreSums[ord];
					if (float.Compare(existing, 0.0f) == 0)
					{
						scoreSums[ord] = current;
						scoreCounts[ord] = 1;
					}
					else
					{
						scoreSums[ord] = scoreSums[ord] + current;
						scoreCounts[ord]++;
					}
				}

				public override float[] GetScoresPerTerm()
				{
					if (scoreCounts != null)
					{
						for (int i = 0; i < scoreCounts.Length; i++)
						{
							scoreSums[i] = scoreSums[i] / scoreCounts[i];
						}
						scoreCounts = null;
					}
					return scoreSums;
				}
			}
		}

		internal class MV : TermsWithScoreCollector
		{
			internal SortedSetDocValues fromDocTermOrds;

			internal readonly BytesRef scratch = new BytesRef();

			internal MV(string field, ScoreMode scoreMode) : base(field, scoreMode)
			{
			}

			// impl that works with multiple values per document
			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				fromDocTermOrds.SetDocument(doc);
				long ord;
				while ((ord = fromDocTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
				{
					fromDocTermOrds.LookupOrd(ord, scratch);
					int termID = collectedTerms.Add(scratch);
					if (termID < 0)
					{
						termID = -termID - 1;
					}
					else
					{
						if (termID >= scoreSums.Length)
						{
							scoreSums = ArrayUtil.Grow(scoreSums);
						}
					}
					switch (scoreMode)
					{
						case ScoreMode.Total:
						{
							scoreSums[termID] += scorer.Score();
							break;
						}

						case ScoreMode.Max:
						{
							scoreSums[termID] = Math.Max(scoreSums[termID], scorer.Score());
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				fromDocTermOrds = FieldCache.DEFAULT.GetDocTermOrds(((AtomicReader)context.Reader
					()), field);
			}

			internal class Avg : TermsWithScoreCollector.MV
			{
				internal int[] scoreCounts = new int[INITIAL_ARRAY_SIZE];

				internal Avg(string field) : base(field, ScoreMode.Avg)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void Collect(int doc)
				{
					fromDocTermOrds.SetDocument(doc);
					long ord;
					while ((ord = fromDocTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						fromDocTermOrds.LookupOrd(ord, scratch);
						int termID = collectedTerms.Add(scratch);
						if (termID < 0)
						{
							termID = -termID - 1;
						}
						else
						{
							if (termID >= scoreSums.Length)
							{
								scoreSums = ArrayUtil.Grow(scoreSums);
								scoreCounts = ArrayUtil.Grow(scoreCounts);
							}
						}
						scoreSums[termID] += scorer.Score();
						scoreCounts[termID]++;
					}
				}

				public override float[] GetScoresPerTerm()
				{
					if (scoreCounts != null)
					{
						for (int i = 0; i < scoreCounts.Length; i++)
						{
							scoreSums[i] = scoreSums[i] / scoreCounts[i];
						}
						scoreCounts = null;
					}
					return scoreSums;
				}
			}
		}
	}
}
