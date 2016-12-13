using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TermInfo = Lucene.Net.Search.VectorHighlight.FieldTermStack.TermInfo;

namespace Lucene.Net.Search.VectorHighlight
{
    /*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

    /// <summary>
    /// <code>FieldTermStack</code> is a stack that keeps query terms in the specified field
    /// of the document to be highlighted.
    /// </summary>
    public class FieldTermStack
    {
        private readonly string fieldName;
        internal List<TermInfo> termList = new List<TermInfo>();

        //public static void main( string[] args ) throws Exception {
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

        /**
         * a constructor.
         * 
         * @param reader IndexReader of the index
         * @param docId document id to be highlighted
         * @param fieldName field of the document to be highlighted
         * @param fieldQuery FieldQuery object
         * @throws IOException If there is a low-level I/O error
         */
        public FieldTermStack(IndexReader reader, int docId, string fieldName, FieldQuery fieldQuery)
        {
            this.fieldName = fieldName;

            ISet<string> termSet = fieldQuery.GetTermSet(fieldName);
            // just return to make null snippet if un-matched fieldName specified when fieldMatch == true
            if (termSet == null) return;

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

            int numDocs = reader.MaxDoc;

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
                float weight = (float)(Math.Log(numDocs / (double)(reader.DocFreq(new Term(fieldName, text)) + 1)) + 1.0);

                int freq = dpEnum.Freq();

                for (int i = 0; i < freq; i++)
                {
                    int pos = dpEnum.NextPosition();
                    if (dpEnum.StartOffset() < 0)
                    {
                        return; // no offsets, null snippet
                    }
                    termList.Add(new TermInfo(term, dpEnum.StartOffset(), dpEnum.EndOffset(), pos, weight));
                }
            }

            // sort by position
            CollectionUtil.TimSort(termList);

            // now look for dups at the same position, linking them together
            int currentPos = -1;
            TermInfo previous = null;
            TermInfo first = null;
            for (int i = 0; i < termList.Count; )
            {
                TermInfo current = termList[i];
                if (current.Position == currentPos)
                {
                    Debug.Assert(previous != null);
                    previous.SetNext(current);
                    previous = current;
                    //iterator.Remove();

                    // LUCENENET NOTE: Remove, but don't advance the i position (since removing will advance to the next item)
                    termList.RemoveAt(i);
                }
                else
                {
                    if (previous != null)
                    {
                        previous.SetNext(first);
                    }
                    previous = first = current;
                    currentPos = current.Position;

                    // LUCENENET NOTE: Only increment the position if we don't do a delete.
                    i++;
                }
            }

            if (previous != null)
            {
                previous.SetNext(first);
            }
        }

        /**
         * @return field name
         */
        public virtual string FieldName
        {
            get { return fieldName; }
        }

        /**
         * @return the top TermInfo object of the stack
         */
        public virtual TermInfo Pop()
        {
            if (termList.Count == 0)
            {
                return null;
            }
            TermInfo first = termList[0];
            termList.Remove(first);
            return first;
        }

        /**
         * @param termInfo the TermInfo object to be put on the top of the stack
         */
        public virtual void Push(TermInfo termInfo)
        {
            termList.Insert(0, termInfo);
        }

        /**
         * to know whether the stack is empty
         * 
         * @return true if the stack is empty, false if not
         */
        public virtual bool IsEmpty
        {
            get { return termList == null || termList.Count == 0; }
        }

        /**
         * Single term with its position/offsets in the document and IDF weight.
         * It is Comparable but considers only position.
         */
        public class TermInfo : IComparable<TermInfo>
        {
            private readonly string text;
            private readonly int startOffset;
            private readonly int endOffset;
            private readonly int position;

            // IDF-weight of this term
            private readonly float weight;

            // pointer to other TermInfo's at the same position.
            // this is a circular list, so with no syns, just points to itself
            private TermInfo next;

            public TermInfo(string text, int startOffset, int endOffset, int position, float weight)
            {
                this.text = text;
                this.startOffset = startOffset;
                this.endOffset = endOffset;
                this.position = position;
                this.weight = weight;
                this.next = this;
            }

            internal void SetNext(TermInfo next) { this.next = next; }
            /** 
             * Returns the next TermInfo at this same position.
             * This is a circular list!
             */
            public virtual TermInfo Next { get { return next; } }
            public virtual string Text { get { return text; } }
            public virtual int StartOffset { get { return startOffset; } }
            public virtual int EndOffset { get { return endOffset; } }
            public virtual int Position { get { return position; } }
            public virtual float Weight { get { return weight; } }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(text).Append('(').Append(startOffset).Append(',').Append(endOffset).Append(',').Append(position).Append(')');
                return sb.ToString();
            }

            public virtual int CompareTo(TermInfo o)
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
                TermInfo other = (TermInfo)obj;
                if (position != other.position)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
