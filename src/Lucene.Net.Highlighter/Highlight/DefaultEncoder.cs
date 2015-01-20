/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search.Highlight;
using Sharpen;

namespace Lucene.Net.Search.Highlight
{
	/// <summary>
	/// Simple
	/// <see cref="Encoder">Encoder</see>
	/// implementation that does not modify the output
	/// </summary>
	public class DefaultEncoder : Encoder
	{
		public DefaultEncoder()
		{
		}

		public virtual string EncodeText(string originalText)
		{
			return originalText;
		}
	}
}
