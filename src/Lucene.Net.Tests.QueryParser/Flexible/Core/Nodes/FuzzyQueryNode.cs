/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="FuzzyQueryNode">FuzzyQueryNode</see>
	/// represents a element that contains
	/// field/text/similarity tuple
	/// </summary>
	public class FuzzyQueryNode : FieldQueryNode
	{
		private float similarity;

		private int prefixLength;

		/// <param name="field">- Field name</param>
		/// <param name="term">- Value</param>
		/// <param name="minSimilarity">- similarity value</param>
		/// <param name="begin">- position in the query string</param>
		/// <param name="end">- position in the query string</param>
		public FuzzyQueryNode(CharSequence field, CharSequence term, float minSimilarity, 
			int begin, int end) : base(field, term, begin, end)
		{
			this.similarity = minSimilarity;
			SetLeaf(true);
		}

		public virtual void SetPrefixLength(int prefixLength)
		{
			this.prefixLength = prefixLength;
		}

		public virtual int GetPrefixLength()
		{
			return this.prefixLength;
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escaper)
		{
			if (IsDefaultField(this.field))
			{
				return GetTermEscaped(escaper) + "~" + this.similarity;
			}
			else
			{
				return this.field + ":" + GetTermEscaped(escaper) + "~" + this.similarity;
			}
		}

		public override string ToString()
		{
			return "<fuzzy field='" + this.field + "' similarity='" + this.similarity + "' term='"
				 + this.text + "'/>";
		}

		public virtual void SetSimilarity(float similarity)
		{
			this.similarity = similarity;
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode clone = (Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
				)base.CloneTree();
			clone.similarity = this.similarity;
			return clone;
		}

		/// <returns>the similarity</returns>
		public virtual float GetSimilarity()
		{
			return this.similarity;
		}
	}
}
