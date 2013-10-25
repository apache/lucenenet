using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class BytesRefFieldSource : FieldCacheSource
    {
        public BytesRefFieldSource(string field)
            : base(field)
        {
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FieldInfo fieldInfo = readerContext.AtomicReader.FieldInfos.FieldInfo(field);
            if (fieldInfo != null && fieldInfo.DocValuesTypeValue == FieldInfo.DocValuesType.BINARY)
            {
                BinaryDocValues binaryValues = Lucene.Net.Search.FieldCache.DEFAULT.GetTerms(readerContext.AtomicReader, field);
                return new AnonymousFunctionValues(this, binaryValues);
            }
            else
            {
                return new AnonymousDocTermsIndexDocValues(this, this, readerContext, field);
            }
        }

        private sealed class AnonymousFunctionValues : FunctionValues
        {
            public AnonymousFunctionValues(BytesRefFieldSource parent, BinaryDocValues binaryValues)
            {
                this.parent = parent;
                this.binaryValues = binaryValues;
            }

            private readonly BytesRefFieldSource parent;
            private readonly BinaryDocValues binaryValues;

            public override bool Exists(int doc)
            {
                return true;
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                binaryValues.Get(doc, target);
                return target.length > 0;
            }

            public override string StrVal(int doc)
            {
                BytesRef bytes = new BytesRef();
                return BytesVal(doc, bytes) ? bytes.Utf8ToString() : null;
            }

            public override Object ObjectVal(int doc)
            {
                return StrVal(doc);
            }

            public override string ToString(int doc)
            {
                return parent.Description + '=' + StrVal(doc);
            }
        }

        private sealed class AnonymousDocTermsIndexDocValues : DocTermsIndexDocValues
        {
            public AnonymousDocTermsIndexDocValues(BytesRefFieldSource parent, ValueSource vs, AtomicReaderContext context, string field)
                : base(vs, context, field)
            {
                this.parent = parent;
            }

            private readonly BytesRefFieldSource parent;

            protected override string ToTerm(string readableValue)
            {
                return readableValue;
            }

            public override Object ObjectVal(int doc)
            {
                return StrVal(doc);
            }

            public override string ToString(int doc)
            {
                return parent.Description + '=' + StrVal(doc);
            }
        }
    }
}
