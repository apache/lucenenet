/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Returns the value of
	/// <see cref="Org.Apache.Lucene.Index.IndexReader.NumDocs()">Org.Apache.Lucene.Index.IndexReader.NumDocs()
	/// 	</see>
	/// for every document. This is the number of documents
	/// excluding deletions.
	/// </summary>
	public class NumDocsValueSource : ValueSource
	{
		public virtual string Name()
		{
			return "numdocs";
		}

		public override string Description()
		{
			return Name() + "()";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			// Searcher has no numdocs so we must use the reader instead
			return new ConstIntDocValues(ReaderUtil.GetTopLevelContext(readerContext).Reader(
				).NumDocs(), this);
		}

		public override bool Equals(object o)
		{
			return this.GetType() == o.GetType();
		}

		public override int GetHashCode()
		{
			return this.GetType().GetHashCode();
		}
	}
}
