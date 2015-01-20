/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.TermRangeFilter">Org.Apache.Lucene.Search.TermRangeFilter
	/// 	</see>
	/// </summary>
	public class RangeFilterBuilder : FilterBuilder
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Filter GetFilter(Element e)
		{
			string fieldName = DOMUtils.GetAttributeWithInheritance(e, "fieldName");
			string lowerTerm = e.GetAttribute("lowerTerm");
			string upperTerm = e.GetAttribute("upperTerm");
			bool includeLower = DOMUtils.GetAttribute(e, "includeLower", true);
			bool includeUpper = DOMUtils.GetAttribute(e, "includeUpper", true);
			return TermRangeFilter.NewStringRange(fieldName, lowerTerm, upperTerm, includeLower
				, includeUpper);
		}
	}
}
