/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Search.Join
{
	/// <summary>
	/// Passed to
	/// <see cref="Org.Apache.Lucene.Search.Collector.SetScorer(Org.Apache.Lucene.Search.Scorer)
	/// 	">Org.Apache.Lucene.Search.Collector.SetScorer(Org.Apache.Lucene.Search.Scorer)</see>
	/// during join collection.
	/// </summary>
	internal sealed class FakeScorer : Scorer
	{
		internal float score;

		internal int doc = -1;

		internal int freq = 1;

		public FakeScorer() : base(null)
		{
		}

		public override int Advance(int target)
		{
			throw new NotSupportedException("FakeScorer doesn't support advance(int)");
		}

		public override int DocID()
		{
			return doc;
		}

		public override int Freq()
		{
			throw new NotSupportedException("FakeScorer doesn't support freq()");
		}

		public override int NextDoc()
		{
			throw new NotSupportedException("FakeScorer doesn't support nextDoc()");
		}

		public override float Score()
		{
			return score;
		}

		public override long Cost()
		{
			return 1;
		}

		public override Weight GetWeight()
		{
			throw new NotSupportedException();
		}

		public override ICollection<Scorer.ChildScorer> GetChildren()
		{
			throw new NotSupportedException();
		}
	}
}
