using Lucene.Net.Analysis.Util;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
{

    /**
    * Factory for {@link KoreanNumberFilter}.
    * <br>
    * <pre class="prettyprint">
    * &lt;fieldType name="text_ko" class="solr.TextField"&gt;
    *   &lt;analyzer&gt;
    *     &lt;tokenizer class="solr.KoreanTokenizerFactory" discardPunctuation="false"/&gt;
    *     &lt;filter class="solr.KoreanNumberFilterFactory"/&gt;
    *   &lt;/analyzer&gt;
    * &lt;/fieldType&gt;
    * </pre>
    * <p>
    * It is important that punctuation is not discarded by the tokenizer so use
    * {@code discardPunctuation="false"} in your {@link KoreanTokenizerFactory}.
    * @since 8.2.0
    * @lucene.spi {@value #NAME}
    */
    public class KoreanNumberFilterFactory : TokenFilterFactory
    {
        ///<summary>SPI name</summary>
        public static readonly string NAME = "koreanNumber";

        /// <summary>
        /// Creates a new KoreanPartOfSpeechStopFilterFactory
        /// </summary>
        public KoreanNumberFilterFactory(Dictionary<string, string> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new IllegalArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new KoreanNumberFilter(input);
        }
    }
}