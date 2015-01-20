/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>
	/// Simple boundary scanner implementation that divides fragments
	/// based on a set of separator characters.
	/// </summary>
	/// <remarks>
	/// Simple boundary scanner implementation that divides fragments
	/// based on a set of separator characters.
	/// </remarks>
	public class SimpleBoundaryScanner : BoundaryScanner
	{
		public const int DEFAULT_MAX_SCAN = 20;

		public static readonly char[] DEFAULT_BOUNDARY_CHARS = new char[] { '.', ',', '!'
			, '?', ' ', '\t', '\n' };

		protected internal int maxScan;

		protected internal ICollection<char> boundaryChars;

		public SimpleBoundaryScanner() : this(DEFAULT_MAX_SCAN, DEFAULT_BOUNDARY_CHARS)
		{
		}

		public SimpleBoundaryScanner(int maxScan) : this(maxScan, DEFAULT_BOUNDARY_CHARS)
		{
		}

		public SimpleBoundaryScanner(char[] boundaryChars) : this(DEFAULT_MAX_SCAN, boundaryChars
			)
		{
		}

		public SimpleBoundaryScanner(int maxScan, char[] boundaryChars)
		{
			this.maxScan = maxScan;
			this.boundaryChars = new HashSet<char>();
			Sharpen.Collections.AddAll(this.boundaryChars, Arrays.AsList(boundaryChars));
		}

		public SimpleBoundaryScanner(int maxScan, ICollection<char> boundaryChars)
		{
			this.maxScan = maxScan;
			this.boundaryChars = boundaryChars;
		}

		public virtual int FindStartOffset(StringBuilder buffer, int start)
		{
			// avoid illegal start offset
			if (start > buffer.Length || start < 1)
			{
				return start;
			}
			int offset;
			int count = maxScan;
			for (offset = start; offset > 0 && count > 0; count--)
			{
				// found?
				if (boundaryChars.Contains(buffer[offset - 1]))
				{
					return offset;
				}
				offset--;
			}
			// if we scanned up to the start of the text, return it, its a "boundary"
			if (offset == 0)
			{
				return 0;
			}
			// not found
			return start;
		}

		public virtual int FindEndOffset(StringBuilder buffer, int start)
		{
			// avoid illegal start offset
			if (start > buffer.Length || start < 0)
			{
				return start;
			}
			int offset;
			int count = maxScan;
			//for( offset = start; offset <= buffer.length() && count > 0; count-- ){
			for (offset = start; offset < buffer.Length && count > 0; count--)
			{
				// found?
				if (boundaryChars.Contains(buffer[offset]))
				{
					return offset;
				}
				offset++;
			}
			// not found
			return start;
		}
	}
}
