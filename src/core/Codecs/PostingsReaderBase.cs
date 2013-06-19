using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class PostingsReaderBase : IDisposable
    {
        protected PostingsReaderBase()
        {
        }

        public abstract void Init(IndexInput termsIn);

        public abstract BlockTermState NewTermState();

        public abstract void NextTerm(FieldInfo fieldInfo, BlockTermState state);

        public abstract DocsEnum Docs(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsEnum reuse, int flags);

        public abstract DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsAndPositionsEnum reuse,
                                                        int flags);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public abstract void ReadTermsBlock(IndexInput termsIn, FieldInfo fieldInfo, BlockTermState termState);
    }
}
