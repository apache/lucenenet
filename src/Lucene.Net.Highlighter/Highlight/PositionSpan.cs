/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Sharpen;

namespace Lucene.Net.Search.Highlight
{
	/// <summary>Utility class to record Positions Spans</summary>
	/// <lucene.internal></lucene.internal>
	public class PositionSpan
	{
		internal int start;

		internal int end;

		public PositionSpan(int start, int end)
		{
			this.start = start;
			this.end = end;
		}
	}
}
