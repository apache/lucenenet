using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class TFValueSource : TermFreqValueSource
    {
        public TFValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name
        {
            get
            {
                return "tf";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            Fields fields = readerContext.AtomicReader.Fields;
            Terms terms = fields.Terms(indexedField);
            IndexSearcher searcher = (IndexSearcher)context["searcher"];
            TFIDFSimilarity similarity = IDFValueSource.AsTFIDF(searcher.Similarity, indexedField);
            if (similarity == null)
            {
                throw new NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }

            return new AnonymousFloatDocValues(this, terms, similarity);
        }

        private sealed class AnonymousDocsEnum : DocsEnum
        {
            public AnonymousDocsEnum(TFValueSource parent)
            {
                this.parent = parent;
            }

            private readonly TFValueSource parent;

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
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
            }

            public override int NextDoc()
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override long Cost
            {
                get
                {
                    return 0;
                }
            }
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(TFValueSource parent, Terms terms, TFIDFSimilarity similarity)
                : base(parent)
            {
                this.parent = parent;
                this.terms = terms;
                this.similarity = similarity;

                Reset();
            }

            private readonly TFValueSource parent;
            private readonly Terms terms;
            private readonly TFIDFSimilarity similarity;

            DocsEnum docs;
            int atDoc;
            int lastDocRequested = -1;

            public void Reset()
            {
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.Iterator(null);
                    if (termsEnum.SeekExact(parent.indexedBytes, false))
                    {
                        docs = termsEnum.Docs(null, null);
                    }
                    else
                    {
                        docs = null;
                    }
                }
                else
                {
                    docs = null;
                }

                if (docs == null)
                {
                    docs = new AnonymousDocsEnum(parent);
                }

                atDoc = -1;
            }

            public override float FloatVal(int doc)
            {
                try
                {
                    if (doc < lastDocRequested)
                    {
                        Reset();
                    }

                    lastDocRequested = doc;
                    if (atDoc < doc)
                    {
                        atDoc = docs.Advance(doc);
                    }

                    if (atDoc > doc)
                    {
                        return similarity.Tf(0);
                    }

                    return similarity.Tf(docs.Freq);
                }
                catch (IOException e)
                {
                    throw new Exception(@"caught exception in function " + parent.Description + @" : doc=" + doc, e);
                }
            }
        }
    }
}
