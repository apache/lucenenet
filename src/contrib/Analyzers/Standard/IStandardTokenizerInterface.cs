using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Standard
{
    public interface IStandardTokenizerInterface
    {
        void GetText(ICharTermAttribute t);

        int YYChar { get; }

        void YYReset(TextReader reader);

        int YYLength { get; }

        int GetNextToken();
    }

    public static class StandardTokenizerInterface
    {
        public const int YYEOF = -1;
    }
}
