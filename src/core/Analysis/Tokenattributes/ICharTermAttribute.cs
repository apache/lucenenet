using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Tokenattributes
{
    public interface ICharTermAttribute : IAttribute, ICharSequence, IAppendable
    {
        void CopyBuffer(char[] buffer, int offset, int length);

        char[] Buffer { get; }

        char[] ResizeBuffer(int newSize);

        ICharTermAttribute SetLength(int length);

        ICharTermAttribute SetEmpty();

        ICharTermAttribute Append(string s);

        ICharTermAttribute Append(string s, int start, int end);

        ICharTermAttribute Append(char c);

        ICharTermAttribute Append(StringBuilder sb);

        ICharTermAttribute Append(StringBuilder sb, int start, int end);

        ICharTermAttribute Append(ICharTermAttribute termAtt);

        ICharTermAttribute Append(ICharSequence csq);

        ICharTermAttribute Append(ICharSequence csq, int start, int end);
    }
}
