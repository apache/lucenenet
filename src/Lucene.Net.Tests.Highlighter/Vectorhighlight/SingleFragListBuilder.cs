/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>
	/// An implementation class of
	/// <see cref="FragListBuilder">FragListBuilder</see>
	/// that generates one
	/// <see cref="WeightedFragInfo">WeightedFragInfo</see>
	/// object.
	/// Typical use case of this class is that you can get an entire field contents
	/// by using both of this class and
	/// <see cref="SimpleFragmentsBuilder">SimpleFragmentsBuilder</see>
	/// .<br/>
	/// <pre class="prettyprint">
	/// FastVectorHighlighter h = new FastVectorHighlighter( true, true,
	/// new SingleFragListBuilder(), new SimpleFragmentsBuilder() );
	/// </pre>
	/// </summary>
	public class SingleFragListBuilder : FragListBuilder
	{
		public virtual FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, 
			int fragCharSize)
		{
			FieldFragList ffl = new SimpleFieldFragList(fragCharSize);
			IList<FieldPhraseList.WeightedPhraseInfo> wpil = new AList<FieldPhraseList.WeightedPhraseInfo
				>();
			Iterator<FieldPhraseList.WeightedPhraseInfo> ite = fieldPhraseList.phraseList.Iterator
				();
			FieldPhraseList.WeightedPhraseInfo phraseInfo = null;
			while (true)
			{
				if (!ite.HasNext())
				{
					break;
				}
				phraseInfo = ite.Next();
				if (phraseInfo == null)
				{
					break;
				}
				wpil.AddItem(phraseInfo);
			}
			if (wpil.Count > 0)
			{
				ffl.Add(0, int.MaxValue, wpil);
			}
			return ffl;
		}
	}
}
