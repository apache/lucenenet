using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class TermFreqValueSource : DocFreqValueSource
    {
        public TermFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name
        {
            get
            {
                return "termfreq";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            Fields fields = readerContext.AtomicReader.Fields;
            Terms terms = fields.Terms(indexedField);
            return new AnonymousIntDocValues(this, fields, terms);
        }

        private sealed class AnonymousDocsEnum : DocsEnum
        {
            public AnonymousDocsEnum(TermFreqValueSource parent)
            {
                this.parent = parent;
            }

            private readonly TermFreqValueSource parent;

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

        private sealed class AnonymousIntDocValues : IntDocValues
        {
            public AnonymousIntDocValues(TermFreqValueSource parent, Fields fields, Terms terms)
                : base(parent)
            {
                this.parent = parent;
                this.fields = fields;
                this.terms = terms;

                Reset();
            }

            private readonly TermFreqValueSource parent;
            private readonly Fields fields;
            private readonly Terms terms;

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

            public override int IntVal(int doc)
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
                        return 0;
                    }

                    return docs.Freq;
                }
                catch (IOException e)
                {
                    throw new Exception(@"caught exception in function " + parent.Description + @" : doc=" + doc, e);
                }
            }
        }
    }
}
