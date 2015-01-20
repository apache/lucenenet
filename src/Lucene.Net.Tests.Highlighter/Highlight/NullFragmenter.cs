/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Search.Highlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// <see cref="Fragmenter">Fragmenter</see>
	/// implementation which does not fragment the text.
	/// This is useful for highlighting the entire content of a document or field.
	/// </summary>
	public class NullFragmenter : Fragmenter
	{
		public virtual void Start(string s, TokenStream tokenStream)
		{
		}

		public virtual bool IsNewFragment()
		{
			return false;
		}
	}
}
