using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class LiteralValueSource : ValueSource
    {
        protected readonly string string_renamed;
        protected readonly BytesRef bytesRef;

        public LiteralValueSource(string string_renamed)
        {
            this.string_renamed = string_renamed;
            this.bytesRef = new BytesRef(string_renamed);
        }

        public virtual string Value
        {
            get
            {
                return string_renamed;
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            return new AnonymousStrDocValues(this);
        }

        private sealed class AnonymousStrDocValues : StrDocValues
        {
            public AnonymousStrDocValues(LiteralValueSource parent)
                : base(parent)
            {
                this.parent = parent;
            }

            private readonly LiteralValueSource parent;

            public override string StrVal(int doc)
            {
                return parent.string_renamed;
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                target.CopyBytes(parent.bytesRef);
                return true;
            }

            public override string ToString(int doc)
            {
                return parent.string_renamed;
            }
        }

        public override string Description
        {
            get 
            {
                return @"literal(" + string_renamed + @")";
            }
        }

        public override bool Equals(Object o)
        {
            if (this == o)
                return true;
            if (!(o is LiteralValueSource))
                return false;
            LiteralValueSource that = (LiteralValueSource) o;
            return string_renamed.Equals(that.string_renamed);
        }

        public static readonly int hash = typeof(LiteralValueSource).GetHashCode();
        
        public override int GetHashCode()
        {
            return hash + string_renamed.GetHashCode();
        }
    }
}
