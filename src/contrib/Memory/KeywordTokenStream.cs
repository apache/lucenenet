using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Index.Memory
{
    public partial class MemoryIndex
    {
        private sealed class KeywordTokenStream<T> : TokenStream
        {
            private IEnumerator<T> iter;
            private int start = 0;
            private TermAttribute termAtt;
            private OffsetAttribute offsetAtt;

            public KeywordTokenStream(IEnumerable<T> keywords)
            {
                iter = keywords.GetEnumerator();
                termAtt = AddAttribute<TermAttribute>();
                offsetAtt = AddAttribute<OffsetAttribute>();
            }

            public override bool IncrementToken()
            {
                if (!iter.MoveNext()) return false;

                T obj = iter.Current;
                if (obj == null)
                    throw new ArgumentException("keyword must not be null");

                String term = obj.ToString();
                ClearAttributes();
                termAtt.SetTermBuffer(term);
                offsetAtt.SetOffset(start, start + termAtt.TermLength());
                start += term.Length + 1; // separate words by 1 (blank) character
                return true;
            }

            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}
