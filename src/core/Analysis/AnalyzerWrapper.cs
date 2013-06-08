using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis
{
    public abstract class AnalyzerWrapper : Analyzer
    {
        protected AnalyzerWrapper()
            : base(new PerFieldReuseStrategy())
        {
        }

        protected abstract Analyzer GetWrappedAnalyzer(string fieldName);

        protected abstract TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components);

        public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
        {
            return WrapComponents(fieldName, GetWrappedAnalyzer(fieldName).CreateComponents(fieldName, reader));
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return GetWrappedAnalyzer(fieldName).GetPositionIncrementGap(fieldName);
        }

        public override int GetOffsetGap(string fieldName)
        {
            return GetWrappedAnalyzer(fieldName).GetOffsetGap(fieldName);
        }

        public override System.IO.TextReader InitReader(string fieldName, System.IO.TextReader reader)
        {
            return GetWrappedAnalyzer(fieldName).InitReader(fieldName, reader);
        }
    }
}
