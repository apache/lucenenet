using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs
{
    public abstract class FieldsProducer : Fields, IDisposable
    {
        protected FieldsProducer()
        {
        }

        public abstract override IEnumerator<string> GetEnumerator();

        public abstract override Terms Terms(string field);

        public abstract override int Size { get; }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
