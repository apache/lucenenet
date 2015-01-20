/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Search.Vectorhighlight;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// A
	/// <see cref="BoundaryScanner">BoundaryScanner</see>
	/// implementation that uses
	/// <see cref="Sharpen.BreakIterator">Sharpen.BreakIterator</see>
	/// to find
	/// boundaries in the text.
	/// </summary>
	/// <seealso cref="Sharpen.BreakIterator">Sharpen.BreakIterator</seealso>
	public class BreakIteratorBoundaryScanner : BoundaryScanner
	{
		internal readonly BreakIterator bi;

		public BreakIteratorBoundaryScanner(BreakIterator bi)
		{
			this.bi = bi;
		}

		public virtual int FindStartOffset(StringBuilder buffer, int start)
		{
			// avoid illegal start offset
			if (start > buffer.Length || start < 1)
			{
				return start;
			}
			bi.SetText(buffer.Substring(0, start));
			bi.Last();
			return bi.Previous();
		}

		public virtual int FindEndOffset(StringBuilder buffer, int start)
		{
			// avoid illegal start offset
			if (start > buffer.Length || start < 0)
			{
				return start;
			}
			bi.SetText(buffer.Substring(start));
			return bi.Next() + start;
		}
	}
}
