/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Misc
{
	/// <summary>
	/// Holder for a term along with its statistics
	/// (
	/// <see cref="docFreq">docFreq</see>
	/// and
	/// <see cref="totalTermFreq">totalTermFreq</see>
	/// ).
	/// </summary>
	public sealed class TermStats
	{
		public BytesRef termtext;

		public string field;

		public int docFreq;

		public long totalTermFreq;

		internal TermStats(string field, BytesRef termtext, int df, long tf)
		{
			this.termtext = BytesRef.DeepCopyOf(termtext);
			this.field = field;
			this.docFreq = df;
			this.totalTermFreq = tf;
		}

		internal string GetTermText()
		{
			return termtext.Utf8ToString();
		}

		public override string ToString()
		{
			return ("TermStats: term=" + termtext.Utf8ToString() + " docFreq=" + docFreq + " totalTermFreq="
				 + totalTermFreq);
		}
	}
}
