using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Index;
using Lucene.Net.Index.Sorter;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Suggest.Analyzing
{
    /// <summary>
    /// Default <see cref="IndexWriterConfig"/> factory for <see cref="AnalyzingInfixSuggester"/>.
    /// </summary>
    public class AnalyzingInfixSuggesterIndexWriterConfigFactory
    {
        private Sort sort;

        /// <summary>
        /// Creates a new config factory that uses the given <see cref="Sort"/> in the sorting merge policy
        /// </summary>
        public AnalyzingInfixSuggesterIndexWriterConfigFactory(Sort sort)
        {
            this.sort = sort;
        }
        
        /// <summary>
        /// Override this to customize index settings, e.g. which
        /// codec to use. 
        /// </summary>
        public virtual IndexWriterConfig Get(LuceneVersion matchVersion, Analyzer indexAnalyzer, OpenMode openMode)
        {
            IndexWriterConfig iwc = new IndexWriterConfig(matchVersion, indexAnalyzer)
            {
                Codec = new Lucene46Codec(),
                OpenMode = openMode
            };

            // This way all merged segments will be sorted at
            // merge time, allow for per-segment early termination
            // when those segments are searched:
            iwc.MergePolicy = new SortingMergePolicy(iwc.MergePolicy, sort);

            return iwc;
        }
    }
}