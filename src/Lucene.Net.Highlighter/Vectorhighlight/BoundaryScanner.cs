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
	/// Finds fragment boundaries: pluggable into
	/// <see cref="BaseFragmentsBuilder">BaseFragmentsBuilder</see>
	/// </summary>
	public interface BoundaryScanner
	{
		/// <summary>Scan backward to find end offset.</summary>
		/// <remarks>Scan backward to find end offset.</remarks>
		/// <param name="buffer">scanned object</param>
		/// <param name="start">start offset to begin</param>
		/// <returns>the found start offset</returns>
		int FindStartOffset(StringBuilder buffer, int start);

		/// <summary>Scan forward to find start offset.</summary>
		/// <remarks>Scan forward to find start offset.</remarks>
		/// <param name="buffer">scanned object</param>
		/// <param name="start">start offset to begin</param>
		/// <returns>the found end offset</returns>
		int FindEndOffset(StringBuilder buffer, int start);
	}
}
