/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search.Vectorhighlight;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// <code>FieldTermStack</code> is a stack that keeps query terms in the specified field
	/// of the document to be highlighted.
	/// </summary>
	/// <remarks>
	/// <code>FieldTermStack</code> is a stack that keeps query terms in the specified field
	/// of the document to be highlighted.
	/// </remarks>
	public class FieldTermStack
	{
		private readonly string fieldName;

		internal List<FieldTermStack.TermInfo> termList = new List<FieldTermStack.TermInfo
			>();

		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		/// <param name="reader">IndexReader of the index</param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fieldQuery">FieldQuery object</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public FieldTermStack(IndexReader reader, int docId, string fieldName, FieldQuery
			 fieldQuery)
		{
			//public static void main( String[] args ) throws Exception {
			//  Analyzer analyzer = new WhitespaceAnalyzer(Version.LUCENE_CURRENT);
			//  QueryParser parser = new QueryParser(Version.LUCENE_CURRENT,  "f", analyzer );
			//  Query query = parser.parse( "a x:b" );
			//  FieldQuery fieldQuery = new FieldQuery( query, true, false );
			//  Directory dir = new RAMDirectory();
			//  IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(Version.LUCENE_CURRENT, analyzer));
			//  Document doc = new Document();
			//  FieldType ft = new FieldType(TextField.TYPE_STORED);
			//  ft.setStoreTermVectors(true);
			//  ft.setStoreTermVectorOffsets(true);
			//  ft.setStoreTermVectorPositions(true);
			//  doc.add( new Field( "f", ft, "a a a b b c a b b c d e f" ) );
			//  doc.add( new Field( "f", ft, "b a b a f" ) );
			//  writer.addDocument( doc );
			//  writer.close();
			//  IndexReader reader = IndexReader.open(dir1);
			//  new FieldTermStack( reader, 0, "f", fieldQuery );
			//  reader.close();
			//}
			this.fieldName = fieldName;
			ICollection<string> termSet = fieldQuery.GetTermSet(fieldName);
			// just return to make null snippet if un-matched fieldName specified when fieldMatch == true
			if (termSet == null)
			{
				return;
			}
			Fields vectors = reader.GetTermVectors(docId);
			if (vectors == null)
			{
				// null snippet
				return;
			}
			Terms vector = vectors.Terms(fieldName);
			if (vector == null)
			{
				// null snippet
				return;
			}
			CharsRef spare = new CharsRef();
			TermsEnum termsEnum = vector.Iterator(null);
			DocsAndPositionsEnum dpEnum = null;
			BytesRef text;
			int numDocs = reader.MaxDoc();
			while ((text = termsEnum.Next()) != null)
			{
				UnicodeUtil.UTF8toUTF16(text, spare);
				string term = spare.ToString();
				if (!termSet.Contains(term))
				{
					continue;
				}
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				if (dpEnum == null)
				{
					// null snippet
					return;
				}
				dpEnum.NextDoc();
				// For weight look here: http://lucene.apache.org/core/3_6_0/api/core/org/apache/lucene/search/DefaultSimilarity.html
				float weight = (float)(Math.Log(numDocs / (double)(reader.DocFreq(new Term(fieldName
					, text)) + 1)) + 1.0);
				int freq = dpEnum.Freq();
				for (int i = 0; i < freq; i++)
				{
					int pos = dpEnum.NextPosition();
					if (dpEnum.StartOffset() < 0)
					{
						return;
					}
					// no offsets, null snippet
					termList.AddItem(new FieldTermStack.TermInfo(term, dpEnum.StartOffset(), dpEnum.EndOffset
						(), pos, weight));
				}
			}
			// sort by position
			termList.Sort();
			// now look for dups at the same position, linking them together
			int currentPos = -1;
			FieldTermStack.TermInfo previous = null;
			FieldTermStack.TermInfo first = null;
			Iterator<FieldTermStack.TermInfo> iterator = termList.Iterator();
			while (iterator.HasNext())
			{
				FieldTermStack.TermInfo current = iterator.Next();
				if (current.position == currentPos)
				{
					previous != null.SetNext(current);
					previous = current;
					iterator.Remove();
				}
				else
				{
					if (previous != null)
					{
						previous.SetNext(first);
					}
					previous = first = current;
					currentPos = current.position;
				}
			}
			if (previous != null)
			{
				previous.SetNext(first);
			}
		}

		/// <returns>field name</returns>
		public virtual string GetFieldName()
		{
			return fieldName;
		}

		/// <returns>the top TermInfo object of the stack</returns>
		public virtual FieldTermStack.TermInfo Pop()
		{
			return termList.Poll();
		}

		/// <param name="termInfo">the TermInfo object to be put on the top of the stack</param>
		public virtual void Push(FieldTermStack.TermInfo termInfo)
		{
			termList.Push(termInfo);
		}

		/// <summary>to know whether the stack is empty</summary>
		/// <returns>true if the stack is empty, false if not</returns>
		public virtual bool IsEmpty()
		{
			return termList == null || termList.Count == 0;
		}

		/// <summary>Single term with its position/offsets in the document and IDF weight.</summary>
		/// <remarks>
		/// Single term with its position/offsets in the document and IDF weight.
		/// It is Comparable but considers only position.
		/// </remarks>
		public class TermInfo : Comparable<FieldTermStack.TermInfo>
		{
			private readonly string text;

			private readonly int startOffset;

			private readonly int endOffset;

			private readonly int position;

			private readonly float weight;

			private FieldTermStack.TermInfo next;

			public TermInfo(string text, int startOffset, int endOffset, int position, float 
				weight)
			{
				// IDF-weight of this term
				// pointer to other TermInfo's at the same position.
				// this is a circular list, so with no syns, just points to itself
				this.text = text;
				this.startOffset = startOffset;
				this.endOffset = endOffset;
				this.position = position;
				this.weight = weight;
				this.next = this;
			}

			internal virtual void SetNext(FieldTermStack.TermInfo next)
			{
				this.next = next;
			}

			/// <summary>Returns the next TermInfo at this same position.</summary>
			/// <remarks>
			/// Returns the next TermInfo at this same position.
			/// This is a circular list!
			/// </remarks>
			public virtual FieldTermStack.TermInfo GetNext()
			{
				return next;
			}

			public virtual string GetText()
			{
				return text;
			}

			public virtual int GetStartOffset()
			{
				return startOffset;
			}

			public virtual int GetEndOffset()
			{
				return endOffset;
			}

			public virtual int GetPosition()
			{
				return position;
			}

			public virtual float GetWeight()
			{
				return weight;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(text).Append('(').Append(startOffset).Append(',').Append(endOffset).Append
					(',').Append(position).Append(')');
				return sb.ToString();
			}

			public virtual int CompareTo(FieldTermStack.TermInfo o)
			{
				return (this.position - o.position);
			}

			public override int GetHashCode()
			{
				int prime = 31;
				int result = 1;
				result = prime * result + position;
				return result;
			}

			public override bool Equals(object obj)
			{
				if (this == obj)
				{
					return true;
				}
				if (obj == null)
				{
					return false;
				}
				if (GetType() != obj.GetType())
				{
					return false;
				}
				FieldTermStack.TermInfo other = (FieldTermStack.TermInfo)obj;
				if (position != other.position)
				{
					return false;
				}
				return true;
			}
		}
	}
}
