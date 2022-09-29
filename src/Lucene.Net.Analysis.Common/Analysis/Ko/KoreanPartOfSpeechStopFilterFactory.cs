using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
{
    /**
     * Factory for {@link KoreanPartOfSpeechStopFilter}.
     * <pre class="prettyprint">
     * &lt;fieldType name="text_ko" class="solr.TextField"&gt;
     *    &lt;analyzer&gt;
     *      &lt;tokenizer class="solr.KoreanTokenizerFactory"/&gt;
     *      &lt;filter class="solr.KoreanPartOfSpeechStopFilterFactory"
     *              tags="E,J"/&gt;
     *    &lt;/analyzer&gt;
     * &lt;/fieldType&gt;
     * </pre>
     *
     * <p>
     * Supports the following attributes:
     * <ul>
     *   <li>tags: List of stop tags. if not specified, {@link KoreanPartOfSpeechStopFilter#DEFAULT_STOP_TAGS} is used.</li>
     * </ul>
     * @lucene.experimental
     *
     * @since 7.4.0
     * @lucene.spi {@value #NAME}
     */
    public class KoreanPartOfSpeechStopFilterFactory: TokenFilterFactory
    {
        public static readonly string NAME = "koreanPartOfSpeechStop";

        private HashSet<POS.Tag> stopTags;

        public KoreanPartOfSpeechStopFilterFactory(Dictionary<string, string> args)
            : base(args)
        {
            ISet<string> stopTagStr = GetSet(args, "tags");
            if (stopTagStr is null)
            {
                stopTags = KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS;
            }
            else
            {
                HashSet<POS.Tag> tmpStopTags = new();
                foreach (string stopTag in stopTagStr)
                {
                    tmpStopTags.Add(POS.Tags[stopTag]);
                }

                stopTags = tmpStopTags;
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new KoreanPartOfSpeechStopFilter(LuceneVersion.LUCENE_48, input, stopTags);
        }
    }
}