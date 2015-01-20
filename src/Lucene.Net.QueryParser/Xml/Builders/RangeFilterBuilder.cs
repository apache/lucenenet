/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Search.TermRangeFilter">Lucene.Net.Search.TermRangeFilter
	/// 	</see>
	/// </summary>
	public class RangeFilterBuilder : FilterBuilder
	{
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
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
