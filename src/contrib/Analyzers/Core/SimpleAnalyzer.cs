using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Core
{
    public sealed class SimpleAnalyzer : Analyzer
    {
        private readonly Version? matchVersion;

        public SimpleAnalyzer(Version? matchVersion)
        {
            this.matchVersion = matchVersion;
        }
        
        public override Analyzer.TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
        {
            return new TokenStreamComponents(new LowerCaseTokenizer(matchVersion, reader));
        }
    }
}
