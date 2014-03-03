using System;
using System.Collections.Generic;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class PerFieldAnalyzerWrapper : AnalyzerWrapper
    {
        private readonly Analyzer defaultAnalyzer;
        private readonly IDictionary<String, Analyzer> fieldAnalyzers;

        public PerFieldAnalyzerWrapper(Analyzer defaultAnalyzer) :this(defaultAnalyzer, null) 
        {
        }

        public PerFieldAnalyzerWrapper(Analyzer defaultAnalyzer, IDictionary<String, Analyzer> fieldAnalyzers)
        {
            this.defaultAnalyzer = defaultAnalyzer;
            this.fieldAnalyzers = fieldAnalyzers ?? Collections.EmptyMap<string, Analyzer>();
        }

        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            var analyzer = fieldAnalyzers[fieldName];
            return analyzer ?? defaultAnalyzer;
        }

        protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            return components;
        }

        public override string ToString()
        {
            return string.Format("PerFieldAnalyzerWrapper({0}, default={1})", fieldAnalyzers, defaultAnalyzer);
        }
    }
}
