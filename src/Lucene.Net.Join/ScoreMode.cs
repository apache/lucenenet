/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search.Join;
using Sharpen;

namespace Lucene.Net.Search.Join
{
	/// <summary>How to aggregate multiple child hit scores into a single parent score.</summary>
	/// <remarks>How to aggregate multiple child hit scores into a single parent score.</remarks>
	public enum ScoreMode
	{
		None,
		Avg,
		Max,
		Total
	}
}
