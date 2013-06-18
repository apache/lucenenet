using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs
{
    public abstract class FieldsConsumer : IDisposable
    {
        protected FieldsConsumer()
        {
        }

        public abstract TermsConsumer AddField(FieldInfo field);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public virtual void Merge(MergeState mergeState, Fields fields)
        {
            foreach (String field in fields.Iterator)
            {
                FieldInfo info = mergeState.fieldInfos.FieldInfo(field);
                //assert info != null : "FieldInfo for field is null: "+ field;
                Terms terms = fields.Terms(field);
                if (terms != null)
                {
                    TermsConsumer termsConsumer = AddField(info);
                    termsConsumer.Merge(mergeState, info.IndexOptions, terms.Iterator(null));
                }
            }
        }
    }
}
