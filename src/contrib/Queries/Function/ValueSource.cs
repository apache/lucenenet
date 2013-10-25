using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function
{
    public abstract class ValueSource
    {
        public abstract FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext);

        public abstract override bool Equals(object obj);

        public abstract override int GetHashCode();

        public abstract string Description { get; }

        public override string ToString()
        {
            return Description;
        }

        public virtual void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
        }

        public static IDictionary<object, object> NewContext(IndexSearcher searcher)
        {
            var context = new IdentityHashMap<object, object>();
            context["searcher"] = searcher;
            return context;
        }

        public virtual SortField GetSortField(bool reverse)
        {
            return new ValueSourceSortField(this, reverse);
        }

        class ValueSourceSortField : SortField
        {
            private readonly ValueSource parent;

            public ValueSourceSortField(ValueSource parent, bool reverse)
                : base(parent.Description, SortField.REWRITEABLE, reverse)
            {
                this.parent = parent;
            }

            public override SortField Rewrite(IndexSearcher searcher)
            {
                var context = NewContext(searcher);
                parent.CreateWeight(context, searcher);
                return new SortField(Field, new ValueSourceComparatorSource(parent, context), Reverse);
            }
        }

        class ValueSourceComparatorSource : FieldComparatorSource
        {
            private readonly IDictionary<object, object> context;
            private readonly ValueSource parent;

            public ValueSourceComparatorSource(ValueSource parent, IDictionary<object, object> context)
            {
                this.parent = parent;
                this.context = context;
            }

            public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
            {
                return new ValueSourceComparator(parent, context, numHits);
            }
        }

        class ValueSourceComparator : FieldComparator<double>
        {
            private readonly double[] values;
            private FunctionValues docVals;
            private double bottom;
            private readonly IDictionary<object, object> fcontext;
            
            private readonly ValueSource parent;

            internal ValueSourceComparator(ValueSource parent, IDictionary<object, object> fcontext, int numHits)
            {
                this.parent = parent;
                this.fcontext = fcontext;
                values = new double[numHits];
            }

            public override int Compare(int slot1, int slot2)
            {
                return values[slot1].CompareTo(values[slot2]);
            }

            public override int CompareBottom(int doc)
            {
                return bottom.CompareTo(docVals.DoubleVal(doc));
            }

            public override void Copy(int slot, int doc)
            {
                values[slot] = docVals.DoubleVal(doc);
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                docVals = parent.GetValues(fcontext, context);
                return this;
            }

            public override void SetBottom(int bottom)
            {
                this.bottom = values[bottom];
            }

            public override object Value(int slot)
            {
                return values[slot];
            }

            public override int CompareDocToValue(int doc, double valueObj)
            {
                double value = valueObj;
                double docValue = docVals.DoubleVal(doc);
                return docValue.CompareTo(value);
            }
        }
    }
}
