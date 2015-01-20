/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="PathQueryNode">PathQueryNode</see>
	/// is used to store queries like
	/// /company/USA/California /product/shoes/brown. QueryText are objects that
	/// contain the text, begin position and end position in the query.
	/// <p>
	/// Example how the text parser creates these objects:
	/// </p>
	/// <pre class="prettyprint">
	/// List values = ArrayList();
	/// values.add(new PathQueryNode.QueryText("company", 1, 7));
	/// values.add(new PathQueryNode.QueryText("USA", 9, 12));
	/// values.add(new PathQueryNode.QueryText("California", 14, 23));
	/// QueryNode q = new PathQueryNode(values);
	/// </pre>
	/// </summary>
	public class PathQueryNode : QueryNodeImpl
	{
		/// <summary>Term text with a beginning and end position</summary>
		public class QueryText : ICloneable
		{
			internal CharSequence value = null;

			/// <summary>!= null The term's begin position.</summary>
			/// <remarks>!= null The term's begin position.</remarks>
			internal int begin;

			/// <summary>The term's end position.</summary>
			/// <remarks>The term's end position.</remarks>
			internal int end;

			/// <param name="value">- text value</param>
			/// <param name="begin">- position in the query string</param>
			/// <param name="end">- position in the query string</param>
			public QueryText(CharSequence value, int begin, int end) : base()
			{
				this.value = value;
				this.begin = begin;
				this.end = end;
			}

			/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
			public virtual PathQueryNode.QueryText Clone()
			{
				PathQueryNode.QueryText clone = (PathQueryNode.QueryText)base.Clone();
				clone.value = this.value;
				clone.begin = this.begin;
				clone.end = this.end;
				return clone;
			}

			/// <returns>the value</returns>
			public virtual CharSequence GetValue()
			{
				return value;
			}

			/// <returns>the begin</returns>
			public virtual int GetBegin()
			{
				return begin;
			}

			/// <returns>the end</returns>
			public virtual int GetEnd()
			{
				return end;
			}

			public override string ToString()
			{
				return value + ", " + begin + ", " + end;
			}
		}

		private IList<PathQueryNode.QueryText> values = null;

		/// <param name="pathElements">- List of QueryText objects</param>
		public PathQueryNode(IList<PathQueryNode.QueryText> pathElements)
		{
			this.values = pathElements;
			if (pathElements.Count <= 1)
			{
				// this should not happen
				throw new RuntimeException("PathQuerynode requires more 2 or more path elements."
					);
			}
		}

		/// <summary>Returns the a List with all QueryText elements</summary>
		/// <returns>QueryText List size</returns>
		public virtual IList<PathQueryNode.QueryText> GetPathElements()
		{
			return values;
		}

		/// <summary>Returns the a List with all QueryText elements</summary>
		public virtual void SetPathElements(IList<PathQueryNode.QueryText> elements)
		{
			this.values = elements;
		}

		/// <summary>Returns the a specific QueryText element</summary>
		/// <returns>QueryText List size</returns>
		public virtual PathQueryNode.QueryText GetPathElement(int index)
		{
			return values[index];
		}

		/// <summary>Returns the CharSequence value of a specific QueryText element</summary>
		/// <returns>the CharSequence for a specific QueryText element</returns>
		public virtual CharSequence GetFirstPathElement()
		{
			return values[0].value;
		}

		/// <summary>Returns a List QueryText element from position startIndex</summary>
		/// <returns>a List QueryText element from position startIndex</returns>
		public virtual IList<PathQueryNode.QueryText> GetPathElements(int startIndex)
		{
			IList<PathQueryNode.QueryText> rValues = new AList<PathQueryNode.QueryText>();
			for (int i = startIndex; i < this.values.Count; i++)
			{
				rValues.AddItem(this.values[i].Clone());
			}
			// this will not happen
			return rValues;
		}

		private CharSequence GetPathString()
		{
			StringBuilder path = new StringBuilder();
			foreach (PathQueryNode.QueryText pathelement in values)
			{
				path.Append("/").Append(pathelement.value);
			}
			return path.ToString();
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escaper)
		{
			StringBuilder path = new StringBuilder();
			path.Append("/").Append(GetFirstPathElement());
			foreach (PathQueryNode.QueryText pathelement in GetPathElements(1))
			{
				CharSequence value = escaper.Escape(pathelement.value, CultureInfo.CurrentCulture
					, EscapeQuerySyntax.Type.STRING);
				path.Append("/\"").Append(value).Append("\"");
			}
			return path.ToString();
		}

		public override string ToString()
		{
			PathQueryNode.QueryText text = this.values[0];
			return "<path start='" + text.begin + "' end='" + text.end + "' path='" + GetPathString
				() + "'/>";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			PathQueryNode clone = (PathQueryNode)base.CloneTree();
			// copy children
			if (this.values != null)
			{
				IList<PathQueryNode.QueryText> localValues = new AList<PathQueryNode.QueryText>();
				foreach (PathQueryNode.QueryText value in this.values)
				{
					localValues.AddItem(value.Clone());
				}
				clone.values = localValues;
			}
			return clone;
		}
	}
}
