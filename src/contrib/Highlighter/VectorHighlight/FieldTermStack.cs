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

using System;
using System.Collections.Generic;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Support;


namespace Lucene.Net.Search.VectorHighlight
{

    /// <summary>
    /// <c>FieldTermStack</c> is a stack that keeps query terms in the specified field
    /// of the document to be highlighted.
    /// </summary>
    public class FieldTermStack
    {
        private readonly String fieldName;
        internal List<TermInfo> termList = new List<TermInfo>();

        //public static void Main(String[] args)
        //{
        //    Analyzer analyzer = new WhitespaceAnalyzer();
        //    QueryParser parser = new QueryParser(Util.Version.LUCENE_CURRENT, "f", analyzer);
        //    Query query = parser.Parse("a x:b");
        //    FieldQuery fieldQuery = new FieldQuery(query, true, false);

        //    Directory dir = new RAMDirectory();
        //    IndexWriter writer = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.LIMITED);
        //    Document doc = new Document();
        //    doc.Add(new Field("f", "a a a b b c a b b c d e f", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
        //    doc.Add(new Field("f", "b a b a f", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
        //    writer.AddDocument(doc);
        //    writer.Close();

        //    IndexReader reader = IndexReader.Open(dir,true);
        //    FieldTermStack ftl = new FieldTermStack(reader, 0, "f", fieldQuery);
        //    reader.Close();
        //}

        /// <summary>
        /// a constructor. 
        /// </summary>
        /// <param name="reader">IndexReader of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fieldQuery">FieldQuery object</param>
#if LUCENENET_350 //Lucene.Net specific code. See https://issues.apache.org/jira/browse/LUCENENET-350
        public FieldTermStack(IndexReader reader, int docId, String fieldName, FieldQuery fieldQuery)
        {
            this.fieldName = fieldName;
            
            List<string> termSet = fieldQuery.getTermSet(fieldName);

            // just return to make null snippet if un-matched fieldName specified when fieldMatch == true
            if (termSet == null) return;

            //TermFreqVector tfv = reader.GetTermFreqVector(docId, fieldName);
            VectorHighlightMapper tfv = new VectorHighlightMapper(termSet);    
            reader.GetTermFreqVector(docId, fieldName, tfv);
                
            if (tfv.Size==0) return; // just return to make null snippets
            
            string[] terms = tfv.GetTerms();
            foreach (String term in terms)
            {
                if (!StringUtils.AnyTermMatch(termSet, term)) continue;
                int index = tfv.IndexOf(term);
                TermVectorOffsetInfo[] tvois = tfv.GetOffsets(index);
                if (tvois == null) return; // just return to make null snippets
                int[] poss = tfv.GetTermPositions(index);
                if (poss == null) return; // just return to make null snippets
                for (int i = 0; i < tvois.Length; i++)
                    termList.AddLast(new TermInfo(term, tvois[i].StartOffset, tvois[i].EndOffset, poss[i]));
            }
            // sort by position
            //Collections.sort(termList);
            Sort(termList);
        }
#else   //Original Port
        public FieldTermStack(IndexReader reader, int docId, String fieldName, FieldQuery fieldQuery)
        {
            this.fieldName = fieldName;

            ISet<String> termSet = fieldQuery.GetTermSet(fieldName);
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
                String term = spare.ToString();
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

                int freq = dpEnum.Freq;

                for (int i = 0; i < freq; i++)
                {
                    int pos = dpEnum.NextPosition();
                    if (dpEnum.StartOffset < 0)
                    {
                        return; // no offsets, null snippet
                    }
                    termList.Add(new TermInfo(term, dpEnum.StartOffset, dpEnum.EndOffset, pos, weight));
                }
            }

            // sort by position
            termList.Sort();
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <value> field name </value>
        public string FieldName
        {
            get { return fieldName; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the top TermInfo object of the stack</returns>
        public TermInfo Pop()
        {
            if (termList.Count == 0) return null;

            TermInfo last = termList[termList.Count - 1];
            termList.RemoveAt(termList.Count - 1);
            return last;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="termInfo">the TermInfo object to be put on the top of the stack</param>
        public void Push(TermInfo termInfo)
        {
            termList.Add(termInfo);
        }

        /// <summary>
        /// to know whether the stack is empty 
        /// </summary>
        /// <returns>true if the stack is empty, false if not</returns>
        public bool IsEmpty()
        {
            return termList == null || termList.Count == 0;
        }

        public class TermInfo : IComparable<TermInfo>
        {
            private readonly String text;
            private readonly int startOffset;
            private readonly int endOffset;
            private readonly int position;

            // IDF-weight of this term
            private readonly float weight;
            
            public TermInfo(String text, int startOffset, int endOffset, int position, float weight)
            {
                this.text = text;
                this.startOffset = startOffset;
                this.endOffset = endOffset;
                this.position = position;
                this.weight = weight;
            }

            public string Text
            {
                get { return text; }
            }

            public int StartOffset
            {
                get { return startOffset; }
            }

            public int EndOffset
            {
                get { return endOffset; }
            }

            public int Position
            {
                get { return position; }
            }

            public float Weight
            {
                get { return weight; }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(text).Append('(').Append(startOffset).Append(',').Append(endOffset).Append(',').Append(position).Append(')');
                return sb.ToString();
            }

            public int CompareTo(TermInfo o)
            {
                return (this.position - o.position);
            }
        }
    }
}
