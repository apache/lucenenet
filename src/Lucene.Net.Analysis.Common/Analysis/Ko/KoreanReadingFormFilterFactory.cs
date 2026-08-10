using Lucene.Net.Analysis.Util;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
{
    /**
    * Factory for {@link KoreanReadingFormFilter}.
    * <pre class="prettyprint">
    * &lt;fieldType name="text_ko" class="solr.TextField"&gt;
    *   &lt;analyzer&gt;
    *     &lt;tokenizer class="solr.KoreanTokenizerFactory"/&gt;
    *     &lt;filter class="solr.KoreanReadingFormFilterFactory"/&gt;
    *   &lt;/analyzer&gt;
    * &lt;/fieldType&gt;
    * </pre>
    * @lucene.experimental
    *
    * @since 7.4.0
    * @lucene.spi {@value #NAME}
    */
    public class KoreanReadingFormFilterFactory : TokenFilterFactory
    {
        ///<summary>SPI name</summary>
        public static readonly string NAME = "koreanReadingForm";

        /// <summary>
        /// Creates a new KoreanReadingFormFilterFactory
        /// </summary>
        public KoreanReadingFormFilterFactory(Dictionary<string, string> args)
            : base(args)
        {
            if (args.Count > 0)
            {
                throw new IllegalArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new KoreanReadingFormFilter(input);
        }

    }
}