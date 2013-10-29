using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.PostingsHighlight
{
    public class PostingsHighlighter
    {
        private static readonly IndexReader EMPTY_INDEXREADER = new MultiReader();
        public static readonly int DEFAULT_MAX_LENGTH = 10000;
        private readonly int maxLength;
        private PassageFormatter defaultFormatter;
        private PassageScorer defaultScorer;

        public PostingsHighlighter()
            : this(DEFAULT_MAX_LENGTH)
        {
        }

        public PostingsHighlighter(int maxLength)
        {
            if (maxLength < 0 || maxLength == int.MaxValue)
            {
                throw new ArgumentException(@"maxLength must be < Integer.MAX_VALUE");
            }

            this.maxLength = maxLength;
        }

        protected virtual BreakIterator GetBreakIterator(string field)
        {
            return BreakIterator.GetSentenceInstance(CultureInfo.CurrentCulture);
        }

        protected virtual PassageFormatter GetFormatter(string field)
        {
            if (defaultFormatter == null)
            {
                defaultFormatter = new DefaultPassageFormatter();
            }

            return defaultFormatter;
        }

        protected virtual PassageScorer GetScorer(string field)
        {
            if (defaultScorer == null)
            {
                defaultScorer = new PassageScorer();
            }

            return defaultScorer;
        }

        public virtual String[] Highlight(string field, Query query, IndexSearcher searcher, TopDocs topDocs)
        {
            return Highlight(field, query, searcher, topDocs, 1);
        }

        public virtual String[] Highlight(string field, Query query, IndexSearcher searcher, TopDocs topDocs, int maxPassages)
        {
            IDictionary<String, String[]> res = HighlightFields(new string[] { field }, query, searcher, topDocs, new int[] { maxPassages });
            return res[field];
        }

        public virtual IDictionary<String, String[]> HighlightFields(string[] fields, Query query, IndexSearcher searcher, TopDocs topDocs)
        {
            int[] maxPassages = new int[fields.Length];
            Arrays.Fill(maxPassages, 1);
            return HighlightFields(fields, query, searcher, topDocs, maxPassages);
        }

        public virtual IDictionary<String, String[]> HighlightFields(string[] fields, Query query, IndexSearcher searcher, TopDocs topDocs, int[] maxPassages)
        {
            ScoreDoc[] scoreDocs = topDocs.ScoreDocs;
            int[] docids = new int[scoreDocs.Length];
            for (int i = 0; i < docids.Length; i++)
            {
                docids[i] = scoreDocs[i].Doc;
            }

            return HighlightFields(fields, query, searcher, docids, maxPassages);
        }

        public virtual IDictionary<String, String[]> HighlightFields(string[] fieldsIn, Query query, IndexSearcher searcher, int[] docidsIn, int[] maxPassagesIn)
        {
            if (fieldsIn.Length < 1)
            {
                throw new ArgumentException(@"fieldsIn must not be empty");
            }

            if (fieldsIn.Length != maxPassagesIn.Length)
            {
                throw new ArgumentException(@"invalid number of maxPassagesIn");
            }

            IndexReader reader = searcher.IndexReader;
            query = Rewrite(query);
            SortedSet<Term> queryTerms = new SortedSet<Term>();
            query.ExtractTerms(queryTerms);
            IndexReaderContext readerContext = reader.Context;
            IList<AtomicReaderContext> leaves = readerContext.Leaves;
            int[] docids = new int[docidsIn.Length];
            Array.Copy(docidsIn, 0, docids, 0, docidsIn.Length);
            string[] fields = new string[fieldsIn.Length];
            Array.Copy(fieldsIn, 0, fields, 0, fieldsIn.Length);
            int[] maxPassages = new int[maxPassagesIn.Length];
            Array.Copy(maxPassagesIn, 0, maxPassages, 0, maxPassagesIn.Length);
            Array.Sort(docids);
            new AnonymousSorterTemplate(this, fields, maxPassages).MergeSort(0, fields.Length - 1);
            String[][] contents = LoadFieldValues(searcher, fields, docids, maxLength);
            IDictionary<String, String[]> highlights = new HashMap<String, String[]>();
            for (int i = 0; i < fields.Length; i++)
            {
                string field = fields[i];
                int numPassages = maxPassages[i];
                Term floor = new Term(field, @"");
                Term ceiling = new Term(field, UnicodeUtil.BIG_TERM);
                SortedSet<Term> fieldTerms = queryTerms.GetViewBetween(floor, ceiling);
                BytesRef[] terms = new BytesRef[fieldTerms.Count];
                int termUpto = 0;
                foreach (Term term in fieldTerms)
                {
                    terms[termUpto++] = term.Bytes;
                }

                IDictionary<int, String> fieldHighlights = HighlightField(field, contents[i], GetBreakIterator(field), terms, docids, leaves, numPassages);
                String[] result = new string[docids.Length];
                for (int j = 0; j < docidsIn.Length; j++)
                {
                    result[j] = fieldHighlights[docidsIn[j]];
                }

                highlights[field] = result;
            }

            return highlights;
        }

        private sealed class AnonymousSorterTemplate : SorterTemplate
        {
            public AnonymousSorterTemplate(PostingsHighlighter parent, string[] fields, int[] maxPassages)
            {
                this.parent = parent;
                this.fields = fields;
                this.maxPassages = maxPassages;
            }

            private readonly PostingsHighlighter parent;
            private readonly string[] fields;
            private readonly int[] maxPassages;

            string pivot;
            
            protected override void Swap(int i, int j)
            {
                string tmp = fields[i];
                fields[i] = fields[j];
                fields[j] = tmp;
                int tmp2 = maxPassages[i];
                maxPassages[i] = maxPassages[j];
                maxPassages[j] = tmp2;
            }

            protected override int Compare(int i, int j)
            {
                return fields[i].CompareTo(fields[j]);
            }

            protected override void SetPivot(int i)
            {
                pivot = fields[i];
            }

            protected override int ComparePivot(int j)
            {
                return pivot.CompareTo(fields[j]);
            }
        }

        protected virtual String[][] LoadFieldValues(IndexSearcher searcher, String[] fields, int[] docids, int maxLength)
        {
            string[][] contents = new string[fields.Length][];
            LimitedStoredFieldVisitor visitor = new LimitedStoredFieldVisitor(fields, maxLength);
            for (int i = 0; i < docids.Length; i++)
            {
                searcher.Doc(docids[i], visitor);
                for (int j = 0; j < fields.Length; j++)
                {
                    contents[j] = contents[j] ?? new string[docids.Length];
                    contents[j][i] = visitor.GetValue(j).ToString();
                }

                visitor.Reset();
            }

            return contents;
        }

        private IDictionary<int, String> HighlightField(string field, string[] contents, BreakIterator bi, BytesRef[] terms, int[] docids, IList<AtomicReaderContext> leaves, int maxPassages)
        {
            IDictionary<int, String> highlights = new HashMap<int, String>();
            DocsAndPositionsEnum[] postings = null;
            TermsEnum termsEnum = null;
            int lastLeaf = -1;
            PassageFormatter fieldFormatter = GetFormatter(field);
            if (fieldFormatter == null)
            {
                throw new NullReferenceException(@"PassageFormatter cannot be null");
            }

            for (int i = 0; i < docids.Length; i++)
            {
                string content = contents[i];
                if (content.Length == 0)
                {
                    continue;
                }

                bi.Text = content;
                int doc = docids[i];
                int leaf = ReaderUtil.SubIndex(doc, leaves);
                AtomicReaderContext subContext = leaves[leaf];
                AtomicReader r = subContext.AtomicReader;
                Terms t = r.Terms(field);
                if (t == null)
                {
                    continue;
                }

                if (leaf != lastLeaf)
                {
                    termsEnum = t.Iterator(null);
                    postings = new DocsAndPositionsEnum[terms.Length];
                }

                Passage[] passages = HighlightDoc(field, terms, content.Length, bi, doc - subContext.docBase, termsEnum, postings, maxPassages);
                if (passages.Length == 0)
                {
                    passages = GetEmptyHighlight(field, bi, maxPassages);
                }

                if (passages.Length > 0)
                {
                    highlights[doc] = fieldFormatter.Format(passages, content);
                }

                lastLeaf = leaf;
            }

            return highlights;
        }

        private Passage[] HighlightDoc(string field, BytesRef[] terms, int contentLength, BreakIterator bi, int doc, TermsEnum termsEnum, DocsAndPositionsEnum[] postings, int n)
        {
            PassageScorer scorer = GetScorer(field);
            if (scorer == null)
            {
                throw new NullReferenceException(@"PassageScorer cannot be null");
            }

            var pq = new Lucene.Net.Support.PriorityQueue<OffsetsEnum>();
            float[] weights = new float[terms.Length];
            for (int i = 0; i < terms.Length; i++)
            {
                DocsAndPositionsEnum de = postings[i];
                int pDoc;
                if (de == EMPTY)
                {
                    continue;
                }
                else if (de == null)
                {
                    postings[i] = EMPTY;
                    if (!termsEnum.SeekExact(terms[i], true))
                    {
                        continue;
                    }

                    de = postings[i] = termsEnum.DocsAndPositions(null, null, DocsAndPositionsEnum.FLAG_OFFSETS);
                    if (de == null)
                    {
                        throw new ArgumentException(@"field '" + field + @"' was indexed without offsets, cannot highlight");
                    }

                    pDoc = de.Advance(doc);
                }
                else
                {
                    pDoc = de.DocID;
                    if (pDoc < doc)
                    {
                        pDoc = de.Advance(doc);
                    }
                }

                if (doc == pDoc)
                {
                    weights[i] = scorer.Weight(contentLength, de.Freq);
                    de.NextPosition();
                    pq.Add(new OffsetsEnum(de, i));
                }
            }

            pq.Add(new OffsetsEnum(EMPTY, int.MaxValue));
            var passageQueue = new Lucene.Net.Support.PriorityQueue<Passage>(n);
            Passage current = new Passage();
            OffsetsEnum off;
            while ((off = pq.Poll()) != null)
            {
                DocsAndPositionsEnum dp = off.dp;
                int start = dp.StartOffset;
                if (start == -1)
                {
                    throw new ArgumentException(@"field '" + field + @"' was indexed without offsets, cannot highlight");
                }

                int end = dp.EndOffset;
                if (start >= current.EndOffset)
                {
                    if (current.StartOffset >= 0)
                    {
                        current.Score *= scorer.Norm(current.StartOffset);
                        if (passageQueue.Count == n && current.Score < passageQueue.Peek().Score)
                        {
                            current.Reset();
                        }
                        else
                        {
                            passageQueue.Offer(current);
                            if (passageQueue.Count > n)
                            {
                                current = passageQueue.Poll();
                                current.Reset();
                            }
                            else
                            {
                                current = new Passage();
                            }
                        }
                    }

                    if (start >= contentLength)
                    {
                        Passage[] passages = passageQueue.ToArray();

                        foreach (Passage p in passages)
                        {
                            p.Sort();
                        }

                        Array.Sort(passages, new AnonymousComparator1(this));
                        return passages;
                    }

                    current.StartOffset = Math.Max(bi.Preceding(start + 1), 0);
                    current.EndOffset = Math.Min(bi.Next(), contentLength);
                }

                int tf = 0;
                while (true)
                {
                    tf++;
                    current.AddMatch(start, end, terms[off.id]);
                    if (off.pos == dp.Freq)
                    {
                        break;
                    }
                    else
                    {
                        off.pos++;
                        dp.NextPosition();
                        start = dp.StartOffset;
                        end = dp.EndOffset;
                    }

                    if (start >= current.EndOffset)
                    {
                        pq.Offer(off);
                        break;
                    }
                }

                current.Score += weights[off.id] * scorer.Tf(tf, current.EndOffset - current.StartOffset);
            }

            ;
            return null;
        }

        private sealed class AnonymousComparator1 : IComparer<Passage>
        {
            public AnonymousComparator1(PostingsHighlighter parent)
            {
                this.parent = parent;
            }

            private readonly PostingsHighlighter parent;
            public int Compare(Passage left, Passage right)
            {
                return left.StartOffset - right.StartOffset;
            }
        }

        protected virtual Passage[] GetEmptyHighlight(string fieldName, BreakIterator bi, int maxPassages)
        {
            List<Passage> passages = new List<Passage>();
            int pos = bi.Current();
            
            while (passages.Count < maxPassages)
            {
                int next = bi.Next();
                if (next == BreakIterator.DONE)
                {
                    break;
                }

                Passage passage = new Passage();
                passage.Score = float.NaN;
                passage.StartOffset = pos;
                passage.EndOffset = next;
                passages.Add(passage);
                pos = next;
            }

            return passages.ToArray();
        }

        private class OffsetsEnum : IComparable<OffsetsEnum>
        {
            internal DocsAndPositionsEnum dp;
            internal int pos;
            internal int id;

            internal OffsetsEnum(DocsAndPositionsEnum dp, int id)
            {
                this.dp = dp;
                this.id = id;
                this.pos = 1;
            }

            public int CompareTo(OffsetsEnum other)
            {
                try
                {
                    int off = dp.StartOffset;
                    int otherOff = other.dp.StartOffset;
                    if (off == otherOff)
                    {
                        return id - other.id;
                    }
                    else
                    {
                        return (((long)off) - otherOff).Signum();
                    }
                }
                catch (IOException)
                {
                    throw;
                }
            }
        }

        private static readonly DocsAndPositionsEnum EMPTY = new AnonymousDocsAndPositionsEnum();

        private sealed class AnonymousDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            public override int NextPosition()
            {
                return 0;
            }

            public override int StartOffset
            {
                get
                {
                    return int.MaxValue;
                }
            }

            public override int EndOffset
            {
                get
                {
                    return int.MaxValue;
                }
            }

            public override BytesRef Payload
            {
                get
                {
                    return null;
                }
            }

            public override int Freq
            {
                get
                {
                    return 0;
                }
            }

            public override int DocID
            {
                get
                {
                    return NO_MORE_DOCS;
                }
            }

            public override int NextDoc()
            {
                return NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                return NO_MORE_DOCS;
            }

            public override long Cost
            {
                get
                {
                    return 0;
                }
            }
        }

        private static Query Rewrite(Query original)
        {
            Query query = original;
            for (Query rewrittenQuery = query.Rewrite(EMPTY_INDEXREADER); rewrittenQuery != query; rewrittenQuery = query.Rewrite(EMPTY_INDEXREADER))
            {
                query = rewrittenQuery;
            }

            return query;
        }

        private class LimitedStoredFieldVisitor : StoredFieldVisitor
        {
            private readonly String[] fields;
            private readonly int maxLength;
            private readonly StringBuilder[] builders;
            private int currentField = -1;

            public LimitedStoredFieldVisitor(string[] fields, int maxLength)
            {
                this.fields = fields;
                this.maxLength = maxLength;
                builders = new StringBuilder[fields.Length];
                for (int i = 0; i < builders.Length; i++)
                {
                    builders[i] = new StringBuilder();
                }
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                StringBuilder builder = builders[currentField];
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                if (builder.Length + value.Length > maxLength)
                {
                    builder.Append(value, 0, maxLength - builder.Length);
                }
                else
                {
                    builder.Append(value);
                }
            }

            public override Status NeedsField(FieldInfo fieldInfo)
            {
                currentField = Array.BinarySearch(fields, fieldInfo.name);
                if (currentField < 0)
                {
                    return Status.NO;
                }
                else if (builders[currentField].Length > maxLength)
                {
                    return fields.Length == 1 ? Status.STOP : Status.NO;
                }

                return Status.YES;
            }

            internal virtual string GetValue(int i)
            {
                return builders[i].ToString();
            }

            internal virtual void Reset()
            {
                currentField = -1;
                for (int i = 0; i < fields.Length; i++)
                {
                    builders[i].Clear();
                }
            }
        }
    }
}
